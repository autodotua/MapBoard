using Esri.ArcGISRuntime.Data;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FieldUtility
    {
        public static IEnumerable<Field> ToEsriFields(this IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                FieldType type = default;
                int length = 0;
                switch (field.Type)
                {
                    case FieldInfoType.Integer:
                        type = FieldType.Int32;
                        length = 9;
                        break;

                    case FieldInfoType.Float:
                        type = FieldType.Float64;
                        length = 13;
                        break;

                    case FieldInfoType.Date:
                        type = FieldType.Date;
                        length = 9;
                        break;

                    case FieldInfoType.Text:
                        type = FieldType.Text;
                        length = 254;
                        break;

                    default:
                        break;
                }
                yield return new Field(type, field.Name, null, length);
            }
        }
    }
}