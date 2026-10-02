// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

/// Generates PostgreSQL commands
module RawQueryParser.Writer

/// Keys containing characters only allowed in double quoted field segments (e.g. "locatedAt.identifier")
/// are written double quoted, brackets are escaped for SqlKata. Plain identifiers are written unchanged.
let private needsQuoting (x: string) =
    x |> Seq.exists (fun c -> c = '.' || c = '[' || c = ']' || c = ':' || c = '/')

let private escapeSqlKataBrackets (x: string) =
    x.Replace("[", "\[").Replace("]", "\]")

let private writePathSegment x =
    if needsQuoting x then $"\"{escapeSqlKataBrackets x}\"" else x

/// <summary>
/// Write a field like
/// <c>Field ["Detail"; "de"; "Title"]</c>
/// as
/// <c>data#>'{Detail,de,Title}'</c>
/// </summary>
let writeRawField (Field fields) =
    fields
    |> List.choose (function
        | IdentifierSegment x -> Some (writePathSegment x)
        | IdentifierArraySegment x -> Some (writePathSegment x)
        | ArraySegment -> None) // Filter away array segment
    |> String.concat ","
    |> sprintf "data#>'\\{%s\\}'"
    
let writeFieldEscaped (Field fields) =
    fields
    |> List.choose (function
        | IdentifierSegment x -> Some (writePathSegment x)
        | IdentifierArraySegment x -> Some (writePathSegment x)
        | ArraySegment -> None) // Filter away array segment
    |> String.concat ","
    |> sprintf "'\\{%s\\}'"

let writeJsonPathField (Field fields) =
    fields
    |> List.map (function
        | IdentifierSegment x -> $".{writePathSegment x}"
        | IdentifierArraySegment x -> $".{writePathSegment x}\[*\]"
        | ArraySegment -> "\[*\]")
    |> String.concat ""
    |> sprintf "$%s"
    
/// <summary>
/// Write a field like
/// <c>Field ["Detail"; "de"; "Title"]</c>
/// as
/// <c>data#>>'{Detail,de,Title}'</c>
/// </summary>
let writeTextField (Field fields) =
    fields
    |> List.choose (function
        | IdentifierSegment x -> Some (writePathSegment x)
        | IdentifierArraySegment x -> Some (writePathSegment x)
        | ArraySegment -> None) // Filter away array segment
    |> String.concat ","
    |> sprintf "data#>>'\\{%s\\}'"

module Sorting =
    open Sorting

    /// <summary>
    /// A sort direction gets written
    /// as <c>ASC</c> or <c>DESC</c>.
    /// </summary>
    let writeDirection = function
        | Ascending -> "ASC"
        | Descending -> "DESC"

    /// A statement gets written by concatenating
    /// a property with a sort direction.
    let writeSortStatement statement =
        let direction = writeDirection statement.Direction
        let field = writeRawField statement.Field
        $"{field} {direction}"

    /// Statements are concatenated <see cref=">writeStatement</c>
    let writeStatements (statements: SortStatements) =
        statements
        |> List.map writeSortStatement
        |> String.concat ", "

module Filtering =
    open Filtering

    let writeOperator = function
        | Eq -> "="
        | Ne -> "<>"
        | Gt -> ">"
        | Ge -> ">="
        | Lt -> "<"
        | Le -> "<="
        | Like -> "ILIKE"

    let writeRawValue = function
        | Boolean x -> box x
        | Number x -> box x
        | String x -> box x
        | DateTime x -> box x
        | Array -> box [||]

    let writeValue = function
        | Boolean true -> "TRUE"
        | Boolean false -> "FALSE"
        | Number value -> $"{value}"
        | String value -> $"'%s{value}'"
        | DateTime value -> $"""'%s{value.ToString("o")}'::timestamp"""
        | Array -> "to_jsonb(array\[\]::varchar\[\])"

    let writeComparison comparison =
        let field =
            match comparison.Value with
            | Boolean _ -> $"({writeRawField comparison.Field})::boolean"
            | Number _ -> $"({writeRawField comparison.Field})::float"
            | String _ -> writeTextField comparison.Field
            | DateTime _ -> $"({writeTextField comparison.Field})::timestamp"
            | Array -> $"({writeRawField comparison.Field})::jsonb"
        let operator = writeOperator comparison.Operator
        let value =
            match comparison.Operator, comparison.Value with
            | Like, String value -> writeValue (String $"%%{value}%%")
            | _, value -> writeValue value
        $"{field} {operator} {value}"

    let rec writeStatement (jsonSerializer: obj -> string) = function 
        | And (left, right) -> $"(%s{writeStatement jsonSerializer left} AND %s{writeStatement jsonSerializer right})"
        | Or (left, right) -> $"(%s{writeStatement jsonSerializer left} OR %s{writeStatement jsonSerializer right})"
        | Condition (Comparison value) -> writeComparison value
        | Condition (In (field, values)) ->
            let escapeBrackets (x: string) =
                x.Replace("[", "\[")
                 .Replace("]", "\]")
                 .Replace("{", "\{")
                 .Replace("}", "\}")
            let writeValue =
                writeRawValue
                >> field.ToNestedObject
                >> jsonSerializer
                >> escapeBrackets
                >> sprintf "data @> '%s'"
            values
            |> List.map writeValue
            |> String.concat " OR "
            |> sprintf "(%s)"
        | Condition (NotIn (field, values)) ->
            $"NOT {writeStatement jsonSerializer (Condition (In (field, values)))}"
        | Condition (LikeIn (field, values)) ->
            let field' = writeJsonPathField field
            let writeValue (value: Value) =
                match value with
                | String s -> s
                | _ -> failwith "Only strings are supported in a likein filter."
                |> fun value -> sprintf $"jsonb_path_exists(data, '%s{field'} ?(@ like_regex \"%s{value}\" flag \"i\")')"
            values
            |> List.map writeValue
            |> String.concat " OR "
            |> sprintf "(%s)"
        | Condition (IsNull property) -> $"{writeTextField property} IS NULL"
        | Condition (IsNotNull property) -> $"{writeTextField property} IS NOT NULL"
