// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Generic
{
    public enum CRUDOperation
    {
        Create,
        Update,
        Delete,
        Read,
        CreateAndUpdate,
    }

    public class DataInfo
    {
        public DataInfo(string table, CRUDOperation operation, bool savechangestoDB = false)
        {
            Table = table;
            Operation = operation;
            SaveChangesToDB = savechangestoDB;

            if (operation == CRUDOperation.Create)
            {
                ErrorWhendataExists = true;
                ErrorWhendataIsNew = false;
            }
            else if (operation == CRUDOperation.Update)
            {
                ErrorWhendataExists = false;
                ErrorWhendataIsNew = true;
            }
            else if (operation == CRUDOperation.CreateAndUpdate)
            {
                ErrorWhendataExists = false;
                ErrorWhendataIsNew = false;
            }
            else
            {
                ErrorWhendataExists = false;
                ErrorWhendataIsNew = false;
            }
        }

        public string Table { get; set; }
        public CRUDOperation Operation { get; set; }
        public bool ErrorWhendataExists { get; set; }
        public bool ErrorWhendataIsNew { get; set; }

        public bool SaveChangesToDB { get; set; }
    }

    public class CompareConfig
    {
        public CompareConfig(bool comparedata, bool compareimages, List<string>? fieldstoignore = null)
        {
            CompareData = comparedata;
            CompareImages = compareimages;

            if (fieldstoignore == null)
                FieldsToIgnore = new List<string>() { 
                    "LastChange", "_Meta", "FirstImport", "RelatedContent"  };
            else
                FieldsToIgnore = fieldstoignore;
        }

        public bool CompareData { get; set; }
        public bool CompareImages { get; set; }

        public List<string> FieldsToIgnore { get; set; }
    }

    public class EditInfo
    {
        public EditInfo(string editor, string? source)
        {
            Editor = editor;
            Source = source;
        }

        public string Editor { get; set; }
        public string? Source { get; set; }
    }

    public class CRUDConstraints
    {
        public CRUDConstraints()
        {
            AccessRole = new List<string>();
        }

        public CRUDConstraints(string? condition, IEnumerable<string> accessRole)
        {
            Condition = condition;
            AccessRole = accessRole;
        }

        public string? Condition { get; set; }
        public IEnumerable<string> AccessRole { get; set; }
    }
}
