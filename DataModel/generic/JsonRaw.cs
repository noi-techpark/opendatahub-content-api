// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Dapper.SqlMapper;

namespace DataModel
{
    public class JsonRawConverter : JsonConverter<JsonRaw>
    {
        public override void WriteJson(JsonWriter writer, JsonRaw? value, JsonSerializer serializer)
        {
            if (value != null)
                writer.WriteRawValue(value.Value);
        }

        public override JsonRaw ReadJson(
            JsonReader reader,
            Type objectType,
            JsonRaw? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            string json = reader.ReadAsString();
            return new JsonRaw(json);

            //Fix that causes Additionalproperties to no more break
            //JToken token = JToken.Load(reader);
            //return new JsonRaw(token.ToString(Formatting.None));
        }
    }

    [JsonConverter(typeof(JsonRawConverter))]
    public class JsonRaw : ICustomQueryParameter
    {
        public JsonRaw(string data)
        {
            Value = data;
        }

        public JsonRaw(object data)
        {
            Value = JsonConvert.SerializeObject(data);
        }

        public string Value { get; }

        public void AddParameter(IDbCommand command, string name)
        {
            var parameter = new NpgsqlParameter(name, NpgsqlDbType.Json);
            parameter.Value = Value;
            command.Parameters.Add(parameter);
        }

        public override string? ToString()
        {
            throw new InvalidOperationException(
                "ToString on JsonRaw shouldn't be called, there is somewhere an implicit ToString() happening (maybe from a manual JSON serialization)."
            );
        }

        public static explicit operator JsonRaw(string x) => new JsonRaw(x);
    }

    public class JsonRawUtils
    {
        public static IEnumerable<JsonRaw> ConvertObjectToJsonRaw<T>(IEnumerable<T> obj)
        {
            return obj.Select(x => new JsonRaw(x)).ToList();
        }
    }
}
