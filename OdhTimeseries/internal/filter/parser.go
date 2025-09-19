package filter

import (
	"fmt"
	"strconv"
	"strings"
)

// FilterExpression represents a parsed filter expression
type FilterExpression struct {
	Type      string             // "condition", "and", "or"
	Condition *FilterCondition   // For leaf conditions
	Children  []FilterExpression // For logical operators
}

// FilterExpressionParser parses legacy filter expressions
type FilterExpressionParser struct{}

// NewFilterExpressionParser creates a new parser
func NewFilterExpressionParser() *FilterExpressionParser {
	return &FilterExpressionParser{}
}

// ParseExpression parses a filter expression string into a FilterExpression tree
func (p *FilterExpressionParser) ParseExpression(expr string) (*FilterExpression, error) {
	if expr == "" {
		return nil, nil
	}

	// Remove outer whitespace
	expr = strings.TrimSpace(expr)

	// Check if it's a logical operation (and/or)
	if strings.HasPrefix(expr, "and(") || strings.HasPrefix(expr, "or(") {
		return p.parseLogicalOperation(expr)
	}

	// It's a simple condition
	condition, err := p.parseCondition(expr)
	if err != nil {
		return nil, err
	}

	return &FilterExpression{
		Type:      "condition",
		Condition: condition,
	}, nil
}

// parseLogicalOperation parses and(...) or or(...) expressions
func (p *FilterExpressionParser) parseLogicalOperation(expr string) (*FilterExpression, error) {
	var operator string
	var content string

	if strings.HasPrefix(expr, "and(") {
		operator = "and"
		content = expr[4:]
	} else if strings.HasPrefix(expr, "or(") {
		operator = "or"
		content = expr[3:]
	} else {
		return nil, fmt.Errorf("invalid logical operation: %s", expr)
	}

	// Remove the closing parenthesis
	if !strings.HasSuffix(content, ")") {
		return nil, fmt.Errorf("missing closing parenthesis in: %s", expr)
	}
	content = content[:len(content)-1]

	// Parse the arguments inside the parentheses
	args, err := p.parseArguments(content)
	if err != nil {
		return nil, fmt.Errorf("error parsing arguments in %s: %w", expr, err)
	}

	// Parse each argument recursively
	var children []FilterExpression
	for _, arg := range args {
		child, err := p.ParseExpression(arg)
		if err != nil {
			return nil, fmt.Errorf("error parsing argument '%s': %w", arg, err)
		}
		if child != nil {
			children = append(children, *child)
		}
	}

	return &FilterExpression{
		Type:     operator,
		Children: children,
	}, nil
}

// parseArguments splits comma-separated arguments, respecting nested parentheses
func (p *FilterExpressionParser) parseArguments(content string) ([]string, error) {
	var args []string
	var current strings.Builder
	depth := 0

	for _, char := range content {
		switch char {
		case '(':
			depth++
			current.WriteRune(char)
		case ')':
			depth--
			current.WriteRune(char)
		case ',':
			if depth == 0 {
				// This comma is at the top level, so it's an argument separator
				args = append(args, strings.TrimSpace(current.String()))
				current.Reset()
			} else {
				current.WriteRune(char)
			}
		default:
			current.WriteRune(char)
		}
	}

	// Add the last argument
	if current.Len() > 0 {
		args = append(args, strings.TrimSpace(current.String()))
	}

	return args, nil
}

// parseCondition parses a single condition like "o2.eq.2" or "temp.key1.key2.gteq.30"
func (p *FilterExpressionParser) parseCondition(expr string) (*FilterCondition, error) {
	// Find the first dot
	firstDot := strings.Index(expr, ".")
	if firstDot == -1 {
		return nil, fmt.Errorf("invalid condition format, expected at least type.operator.value: %s", expr)
	}

	condition := &FilterCondition{
		Alias: expr[:firstDot], // This is the type name
	}
	remainder := expr[firstDot+1:]

	// Known operators that should be recognized
	knownOperators := []string{"eq", "neq", "lt", "gt", "lteq", "gteq", "re", "ire", "nre", "nire", "in", "nin", "bbi", "bbc", "dlt"}

	// Find the last occurrence of any known operator followed by a dot
	var operatorPos = -1
	var foundOperator string

	// Search from right to left to find the rightmost operator
	for i := len(remainder) - 1; i >= 0; i-- {
		if remainder[i] == '.' {
			// Check if what comes before this dot is a known operator
			for _, op := range knownOperators {
				if i >= len(op) && remainder[i-len(op):i] == op {
					// Check that it's preceded by a dot or is at the start
					if i-len(op) == 0 || remainder[i-len(op)-1] == '.' {
						operatorPos = i - len(op)
						foundOperator = op
						break
					}
				}
			}
			if foundOperator != "" {
				break
			}
		}
	}

	if operatorPos == -1 {
		return nil, fmt.Errorf("no valid operator found in expression: %s", expr)
	}

	// Split into path, operator, and value
	var pathPart string
	var valuePart string

	if operatorPos > 0 {
		// JSON path case: there's something before the operator
		pathPart = remainder[:operatorPos-1] // Everything before the dot before the operator
		condition.JSONPath = strings.Split(pathPart, ".")
	}

	// Value starts after operator and the following dot
	valuePart = remainder[operatorPos+len(foundOperator)+1:]

	// Set operator
	condition.Operator = FilterOperator(foundOperator)

	// Handle special operators that need list parsing before general parsing
	switch condition.Operator {
	case OpIn, OpNotIn:
		condition.Value = p.parseList(valuePart)
	case OpBoundingBoxIntersect, OpBoundingBoxContain, OpDistanceLessThan:
		condition.Value = p.parseCoordinateList(valuePart)
	default:
		condition.Value = p.parseValue(valuePart)
	}

	return condition, nil
}

// parseValue parses a string value into appropriate type
func (p *FilterExpressionParser) parseValue(valueStr string) interface{} {
	// Handle null
	if valueStr == "null" {
		return nil
	}

	// Handle boolean
	if valueStr == "true" {
		return true
	}
	if valueStr == "false" {
		return false
	}

	// Handle quoted strings
	if len(valueStr) >= 2 && valueStr[0] == '"' && valueStr[len(valueStr)-1] == '"' {
		return valueStr[1 : len(valueStr)-1]
	}

	// Handle lists (value1,value2,value3)
	if strings.HasPrefix(valueStr, "(") && strings.HasSuffix(valueStr, ")") {
		return p.parseList(valueStr)
	}

	// Try to parse as number
	if num, err := strconv.ParseFloat(valueStr, 64); err == nil {
		return num
	}

	// Default to string
	return valueStr
}

// parseList parses a list like "(value1,value2,value3)" into a slice
func (p *FilterExpressionParser) parseList(listStr string) []interface{} {
	if !strings.HasPrefix(listStr, "(") || !strings.HasSuffix(listStr, ")") {
		return []interface{}{listStr}
	}

	content := listStr[1 : len(listStr)-1]
	if content == "" {
		return []interface{}{}
	}

	parts := strings.Split(content, ",")
	result := make([]interface{}, len(parts))
	for i, part := range parts {
		result[i] = p.parseValue(strings.TrimSpace(part))
	}
	return result
}

// parseCoordinateList parses coordinate lists for spatial operations
func (p *FilterExpressionParser) parseCoordinateList(listStr string) []float64 {
	if !strings.HasPrefix(listStr, "(") || !strings.HasSuffix(listStr, ")") {
		return []float64{}
	}

	content := listStr[1 : len(listStr)-1]
	parts := strings.Split(content, ",")
	result := make([]float64, 0, len(parts))

	for _, part := range parts {
		part = strings.TrimSpace(part)
		if val, err := strconv.ParseFloat(part, 64); err == nil {
			result = append(result, val)
		}
	}

	return result
}

// ConvertToValueConditions converts a FilterExpression tree to ValueCondition slice for processing
func (p *FilterExpressionParser) ConvertToValueConditions(expr *FilterExpression) ([]ValueCondition, error) {
	if expr == nil {
		return nil, nil
	}

	var conditions []ValueCondition

	switch expr.Type {
	case "condition":
		if expr.Condition != nil {
			condition := ValueCondition{
				TypeName: expr.Condition.Alias,
				Operator: expr.Condition.Operator,
				Value:    expr.Condition.Value,
				JSONPath: expr.Condition.JSONPath,
			}
			conditions = append(conditions, condition)
		}
	case "and", "or":
		// For logical operations, we collect all conditions
		// The actual AND/OR logic will be implemented in the SQL generation
		for _, child := range expr.Children {
			childConditions, err := p.ConvertToValueConditions(&child)
			if err != nil {
				return nil, err
			}
			conditions = append(conditions, childConditions...)
		}
	}

	return conditions, nil
}