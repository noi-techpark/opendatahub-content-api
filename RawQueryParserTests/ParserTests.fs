// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

module RawQueryParser.ParserTests

open Expecto
open Parser

[<Tests>]
let parserTests =
    testList "Parser" [
        testList "Field" [
            test "Simple field should parse correctly" {
                let expected = Ok (Field [ IdentifierSegment "Detail" ])
                let actual = run field "Detail"
                Expect.equal actual expected ""
            }
            test "Hierarchial field should parse correctly" {
                let expected = Ok (Field (List.map IdentifierSegment [ "Detail"; "de"; "Title" ]))
                let actual = run field "Detail.de.Title"
                Expect.equal actual expected ""
            }
            test "Field with snake case" {
                let expected = Ok (Field [IdentifierSegment "Detail_Field"])
                let actual = run field "Detail_Field"
                Expect.equal actual expected ""
            }
            test "Field with kebab case" {
                let expected = Ok (Field [IdentifierSegment "Detail-Field"])
                let actual = run field "Detail-Field"
                Expect.equal actual expected ""
            }
            test "Field with .[] array syntax" {
                let expected = Ok (Field [IdentifierSegment "Features"; ArraySegment; IdentifierSegment "Id"])
                let actual = run field "Features.[].Id"
                Expect.equal actual expected ""
            }
            test "Field with .[*] array syntax" {
                let expected = Ok (Field [IdentifierSegment "Features"; ArraySegment; IdentifierSegment "Id"])
                let actual = run field "Features.[*].Id"
                Expect.equal actual expected ""
            }
            test "Field with [*] array syntax (JSON path syntax)" {
                let expected = Ok (Field [IdentifierArraySegment "Features"; IdentifierSegment "Id"])
                let actual = run field "Features[*].Id"
                Expect.equal actual expected ""
            }
            test "Field with quoted key containing a dot" {
                let expected = Ok (Field (List.map IdentifierSegment [ "Mapping"; "discoverswiss"; "locatedAt.identifier" ]))
                let actual = run field "Mapping.discoverswiss.\"locatedAt.identifier\""
                Expect.equal actual expected ""
            }
            test "Field with quoted key containing brackets" {
                let expected = Ok (Field (List.map IdentifierSegment [ "Mapping"; "discoverswiss"; "potentialAction[0].additionalType" ]))
                let actual = run field "Mapping.discoverswiss.\"potentialAction[0].additionalType\""
                Expect.equal actual expected ""
            }
            test "Field with quoted key in the middle" {
                let expected = Ok (Field (List.map IdentifierSegment [ "Foo"; "a.b"; "Bar" ]))
                let actual = run field "Foo.\"a.b\".Bar"
                Expect.equal actual expected ""
            }
            test "Field with quoted key and [*] array syntax" {
                let expected = Ok (Field [IdentifierArraySegment "a.b"; IdentifierSegment "Id"])
                let actual = run field "\"a.b\"[*].Id"
                Expect.equal actual expected ""
            }
            test "Field with quoted key containing a single quote should fail" {
                let actual = run Filtering.statement "isnotnull(Foo.\"a'b\")"
                Expect.isError actual ""
            }
            test "Field with empty quoted key should fail" {
                let actual = run Filtering.statement "isnotnull(Foo.\"\")"
                Expect.isError actual ""
            }
        ]
        testList "Sorting" [
            testList "Sort order" [
                test "Order ascending" {
                    let actual = run Sorting.orderBy ""
                    Expect.equal actual (Ok Sorting.Ascending) ""
                }
                test "Order descending" {
                    let actual = run Sorting.orderBy "-"
                    Expect.equal actual (Ok Sorting.Descending) ""
                }
            ]
            testList "Sort order and field" [
                test "Sort by single" {
                    let expected: Result<Sorting.SortStatements, _> = Ok [
                        { Field = Field (List.map IdentifierSegment ["Detail"; "de"; "Title"]); Direction = Sorting.Descending }
                    ]
                    let actual = run Sorting.statements "-Detail.de.Title"
                    Expect.equal actual expected ""
                }
                test "Sort by multiple" {
                    let expected: Result<Sorting.SortStatements, _> = Ok [
                        { Field = Field (List.map IdentifierSegment ["Detail"; "de"; "Title"]); Direction = Sorting.Descending }
                        { Field = Field (List.map IdentifierSegment ["Detail"; "de"; "Body"]); Direction = Sorting.Ascending }
                    ]
                    let actual = run Sorting.statements "-Detail.de.Title,Detail.de.Body"
                    Expect.equal actual expected ""
                }
            ]
        ]
        testList "Filtering" [
            test "Simple condition with boolean" {
                let expected =
                    Ok (Comp { Field = Field [IdentifierSegment "Active"]
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.Boolean true })
                let actual = run Filtering.statement "eq(Active, true)"
                Expect.equal actual expected ""
            }
            test "Simple condition with number" {
                let expected =
                    Ok (Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.Number 200. })
                let actual = run Filtering.statement "eq(Geo.Altitude, 200)"
                Expect.equal actual expected ""
            }
            test "Simple condition with single quote string" {
                let expected =
                    Ok (Comp { Field = Field (List.map IdentifierSegment ["Detail"; "de"; "Title"])
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.String "Foo" })
                let actual = run Filtering.statement "eq(Detail.de.Title, 'Foo')"
                Expect.equal actual expected ""
            }
            test "Simple condition with double quote string" {
                let expected =
                    Ok (Comp { Field = Field (List.map IdentifierSegment ["Detail"; "de"; "Title"])
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.String "Foo" })
                let actual = run Filtering.statement """eq(Detail.de.Title, "Foo")"""
                Expect.equal actual expected ""
            }
            test "Simple condition with date time" {
                let expected =
                    Ok (Comp { Field = Field [ IdentifierSegment "LastChange" ]
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.DateTime (System.DateTime(2010,01,01)) })
                let actual = run Filtering.statement """eq(LastChange, dt"2010-01-01")"""
                Expect.equal actual expected ""
            }
            test "Simple condition with array" {
                let expected =
                    Ok (Comp { Field = Field [ IdentifierSegment "ImageGallery" ]
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.Array })
                let actual = run Filtering.statement """eq(ImageGallery, [])"""
                Expect.equal actual expected ""
            }
            test "Simple condition with array alternative notation" {
                let expected =
                    Ok (Comp { Field = Field [ IdentifierSegment "ImageGallery" ]
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.Array })
                let actual = run Filtering.statement """eq(ImageGallery, [*])"""
                Expect.equal actual expected ""
            }
            test "AND condition" {
                let expected =
                    Ok (
                        And (
                            Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                   Operator = Filtering.Operator.Ge
                                   Value = Filtering.Number 200. },
                            Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                   Operator = Filtering.Operator.Le
                                   Value = Filtering.Number 400. }
                        )
                    )
                let actual = run Filtering.statement "and(ge(Geo.Altitude, 200), le(Geo.Altitude, 400))"
                Expect.equal actual expected ""
            }
            test "AND with multiple conditions" {
                let expected =
                    Ok (
                        And (
                            Comp { Field = Field (List.map IdentifierSegment ["Active"])
                                   Operator = Filtering.Operator.Eq
                                   Value = Filtering.Boolean true },
                            And (
                                Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                       Operator = Filtering.Operator.Ge
                                       Value = Filtering.Number 200. },
                                Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                       Operator = Filtering.Operator.Le
                                       Value = Filtering.Number 400. }
                            )
                        )
                    )
                let actual = run Filtering.statement "and(eq(Active, true), ge(Geo.Altitude, 200), le(Geo.Altitude, 400))"
                Expect.equal actual expected ""
            }
            test "AND with nested OR" {
                let expected =
                    Ok (
                        Or (
                            Comp { Field = Field [IdentifierSegment "Active"]
                                   Operator = Filtering.Operator.Eq
                                   Value = Filtering.Boolean true },
                            And (
                                Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                       Operator = Filtering.Operator.Ge
                                       Value = Filtering.Number 200. },
                                Comp { Field = Field (List.map IdentifierSegment ["Geo"; "Altitude"])
                                       Operator = Filtering.Operator.Le
                                       Value = Filtering.Number 400. }
                            )
                        )
                    )
                let actual = run Filtering.statement "or(eq(Active, true), and(ge(Geo.Altitude, 200), le(Geo.Altitude, 400)))"
                Expect.equal actual expected ""
            }
            test "LIKE" {
                let expected = Ok <| Comp {
                    Field = Field [IdentifierSegment "Title"]
                    Operator = Filtering.Operator.Like
                    Value = Filtering.String "Ski"
                }
                let actual = run Filtering.statement "like(Title, 'Ski')"
                Expect.equal actual expected ""
            }
            test "Condition with NULL check" {
                let expected =
                    Ok (Cond (Filtering.IsNull (Field (List.map IdentifierSegment ["Detail"; "ru"; "Title"]))))
                let actual = run Filtering.statement "isnull(Detail.ru.Title)"
                Expect.equal actual expected ""
            }
            test "Condition with IN" {
                let expected =
                    Ok (
                        In (Field [IdentifierSegment "HasLanguage"], [Filtering.String "de"; Filtering.String "it"])
                    )
                let actual = run Filtering.statement "in(HasLanguage,'de','it')"
                Expect.equal actual expected ""
            }
            test "Condition with LIKEIN" {
                let expected =
                    Ok (
                        LikeIn (Field [IdentifierSegment "OdhTags"], [Filtering.String "Ski"; Filtering.String "Winter"])
                    )
                let actual = run Filtering.statement "likein(OdhTags, 'Ski', 'Winter')"
                Expect.equal actual expected ""
            }
            test "Array syntax" {
                let expected =
                    Ok (
                        Comp { Field = Field [IdentifierSegment "Features"; ArraySegment; IdentifierSegment "Id"]
                               Operator = Filtering.Operator.Eq
                               Value = Filtering.Number 42. }
                    )
                let actual = run Filtering.statement "eq(Features.[].Id,42)"
                Expect.equal actual expected ""
            }
        ]
    ]

