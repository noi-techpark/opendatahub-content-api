/**
 * Build rawfilter expression for Content API
 */
export class ContentFilterBuilder {
  constructor() {
    this.conditions = []
  }

  /**
   * Add equality condition
   */
  eq(field, value) {
    const formattedValue = typeof value === 'string' ? `'${value}'` : value
    this.conditions.push(`eq(${field},${formattedValue})`)
    return this
  }

  /**
   * Add not equal condition
   */
  ne(field, value) {
    const formattedValue = typeof value === 'string' ? `'${value}'` : value
    this.conditions.push(`ne(${field},${formattedValue})`)
    return this
  }

  /**
   * Add greater than condition
   */
  gt(field, value) {
    this.conditions.push(`gt(${field},${value})`)
    return this
  }

  /**
   * Add less than condition
   */
  lt(field, value) {
    this.conditions.push(`lt(${field},${value})`)
    return this
  }

  /**
   * Add is not null condition
   */
  isNotNull(field) {
    this.conditions.push(`isnotnull(${field})`)
    return this
  }

  /**
   * Add is null condition
   */
  isNull(field) {
    this.conditions.push(`isnull(${field})`)
    return this
  }

  /**
   * Add like condition
   */
  like(field, pattern) {
    this.conditions.push(`like(${field},'${pattern}')`)
    return this
  }

  /**
   * Add in condition
   */
  in(field, value) {
    const formattedValue = typeof value === 'string' ? `'${value}'` : value
    this.conditions.push(`in(${field},${formattedValue})`)
    return this
  }

  /**
   * Build the final filter string
   */
  build() {
    if (this.conditions.length === 0) return ''
    if (this.conditions.length === 1) return this.conditions[0]
    return `and(${this.conditions.join(',')})`
  }

  /**
   * Reset the builder
   */
  reset() {
    this.conditions = []
    return this
  }
}

/**
 * Build filter expression for Timeseries API
 */
export class TimeseriesFilterBuilder {
  constructor() {
    this.conditions = []
  }

  /**
   * Add equality condition
   */
  eq(timeseries, value) {
    this.conditions.push(`${timeseries}.eq.${value}`)
    return this
  }

  /**
   * Add not equal condition
   */
  neq(timeseries, value) {
    this.conditions.push(`${timeseries}.neq.${value}`)
    return this
  }

  /**
   * Add greater than condition
   */
  gt(timeseries, value) {
    this.conditions.push(`${timeseries}.gt.${value}`)
    return this
  }

  /**
   * Add less than condition
   */
  lt(timeseries, value) {
    this.conditions.push(`${timeseries}.lt.${value}`)
    return this
  }

  /**
   * Add greater than or equal condition
   */
  gteq(timeseries, value) {
    this.conditions.push(`${timeseries}.gteq.${value}`)
    return this
  }

  /**
   * Add less than or equal condition
   */
  lteq(timeseries, value) {
    this.conditions.push(`${timeseries}.lteq.${value}`)
    return this
  }

  /**
   * Add in condition
   */
  in(timeseries, values) {
    const valueList = Array.isArray(values) ? values.join(',') : values
    this.conditions.push(`${timeseries}.in.(${valueList})`)
    return this
  }

  /**
   * Add regular expression condition
   */
  re(timeseries, pattern) {
    this.conditions.push(`${timeseries}.re.${pattern}`)
    return this
  }

  /**
   * Add bounding box intersecting condition
   */
  bbi(timeseries, leftX, leftY, rightX, rightY, srid = 4326) {
    this.conditions.push(`${timeseries}.bbi.(${leftX},${leftY},${rightX},${rightY},${srid})`)
    return this
  }

  /**
   * Add distance less than condition
   */
  dlt(timeseries, distance, pointX, pointY, srid = 4326) {
    this.conditions.push(`${timeseries}.dlt.(${distance},${pointX},${pointY},${srid})`)
    return this
  }

  /**
   * Build the final filter string with AND logic
   */
  buildAnd() {
    if (this.conditions.length === 0) return ''
    if (this.conditions.length === 1) return this.conditions[0]
    return `and(${this.conditions.join(', ')})`
  }

  /**
   * Build the final filter string with OR logic
   */
  buildOr() {
    if (this.conditions.length === 0) return ''
    if (this.conditions.length === 1) return this.conditions[0]
    return `or(${this.conditions.join(', ')})`
  }

  /**
   * Reset the builder
   */
  reset() {
    this.conditions = []
    return this
  }
}

/**
 * Parse rawfilter string into human-readable format
 */
export function parseRawFilter(rawfilter) {
  if (!rawfilter) return 'No filter'

  // Simple parsing for display purposes
  return rawfilter
    .replace(/and\(/g, 'AND (')
    .replace(/or\(/g, 'OR (')
    .replace(/eq\(/g, 'equals(')
    .replace(/ne\(/g, 'not equals(')
    .replace(/gt\(/g, 'greater than(')
    .replace(/lt\(/g, 'less than(')
    .replace(/isnotnull\(/g, 'is not null(')
    .replace(/isnull\(/g, 'is null(')
}
