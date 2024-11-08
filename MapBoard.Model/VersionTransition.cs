using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Model
{
    /// <summary>
    /// 版本迁移
    /// </summary>
    public class VersionTransition
    {
        /// <summary>
        /// 迁移旧版的Symbols到新版的Renderer
        /// </summary>
        /// <param name="jLayer"></param>
        /// <param name="layer"></param>
        public static void V20220222_SymbolsToRenderer(JObject jLayer, LayerInfo layer)
        {
            string symbolsKey = "Symbols";
            if (jLayer.ContainsKey(symbolsKey) 
                && !layer.Renderer.HasCustomSymbols)
            {
                try
                {
                    var symbols = jLayer[symbolsKey].ToObject<Dictionary<string, SymbolInfo>>();
                    if (symbols==null||symbols.Count == 0)
                    {
                        {
                            return;
                        }
                    }
                    layer.Renderer.Symbols.Clear();
                    layer.Renderer.KeyFieldName = "Key";
                    foreach (var key in symbols.Keys)
                    {
                        if (key == "")
                        {
                            layer.Renderer.DefaultSymbol = symbols[key];
                        }
                        else
                        {
                            layer.Renderer.Symbols.Add(key, symbols[key]);
                        }
                    }


                    layer.Fields = layer.Fields.Concat(new FieldInfo[] {
                        new FieldInfo("Info","标签",FieldInfoType.Text),
                        new FieldInfo("Key","分类",FieldInfoType.Text),
                        new FieldInfo("Date","日期",FieldInfoType.Date),
                        new FieldInfo("CrtTime","创建时间",FieldInfoType.DateTime),
                    }).ToArray();
                }
                catch (Exception ex)
                {
                }
            }

        }
    }
}
