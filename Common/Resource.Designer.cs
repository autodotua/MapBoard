﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace MapBoard.Common {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resource() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MapBoard.Common.Resource", typeof(Resource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性，对
        ///   使用此强类型资源类的所有资源查找执行重写。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 Key 的本地化字符串。
        /// </summary>
        public static string ClassFieldName {
            get {
                return ResourceManager.GetString("ClassFieldName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Date 的本地化字符串。
        /// </summary>
        public static string DateFieldName {
            get {
                return ResourceManager.GetString("DateFieldName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Info 的本地化字符串。
        /// </summary>
        public static string LabelFieldName {
            get {
                return ResourceManager.GetString("LabelFieldName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 {
        ///  &quot;labelExpressionInfo&quot;: {
        ///    &quot;expression&quot;: &quot;$expression&quot;
        ///  },
        ///  &quot;maxScale&quot;: 0,
        ///  &quot;minScale&quot;: 0,
        ///  &quot;symbol&quot;: {
        ///    &quot;angle&quot;: 0,
        ///    &quot;backgroundColor&quot;: [
        ///      0,
        ///      0,
        ///      0,
        ///      0
        ///    ],
        ///    &quot;borderLineColor&quot;: [
        ///      0,
        ///      0,
        ///      0,
        ///      0
        ///    ],
        ///    &quot;borderLineSize&quot;: 0,
        ///    &quot;color&quot;: [
        ///      0,
        ///      0,
        ///      0,
        ///      255
        ///    ],
        ///    &quot;font&quot;: {
        ///      &quot;decoration&quot;: &quot;none&quot;,
        ///      &quot;size&quot;: 15,
        ///      &quot;style&quot;: &quot;normal&quot;,
        ///      &quot;weight&quot;: &quot;normal&quot;
        ///    },
        ///    &quot;haloColor [字符串的其余部分被截断]&quot;; 的本地化字符串。
        /// </summary>
        public static string LabelJson {
            get {
                return ResourceManager.GetString("LabelJson", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 PROJCS[&quot;WGS 84 / Pseudo-Mercator&quot;,
        ///    GEOGCS[&quot;WGS 84&quot;,
        ///        DATUM[&quot;WGS_1984&quot;,
        ///            SPHEROID[&quot;WGS 84&quot;,6378137,298.257223563,
        ///                AUTHORITY[&quot;EPSG&quot;,&quot;7030&quot;]],
        ///            AUTHORITY[&quot;EPSG&quot;,&quot;6326&quot;]],
        ///        PRIMEM[&quot;Greenwich&quot;,0,
        ///            AUTHORITY[&quot;EPSG&quot;,&quot;8901&quot;]],
        ///        UNIT[&quot;degree&quot;,0.0174532925199433,
        ///            AUTHORITY[&quot;EPSG&quot;,&quot;9122&quot;]],
        ///        AUTHORITY[&quot;EPSG&quot;,&quot;4326&quot;]],
        ///    PROJECTION[&quot;Mercator_1SP&quot;],
        ///    PARAMETER[&quot;central_meridian&quot;,0],
        ///    PARAMETER[&quot;scale_factor&quot; [字符串的其余部分被截断]&quot;; 的本地化字符串。
        /// </summary>
        public static string Proj3857 {
            get {
                return ResourceManager.GetString("Proj3857", resourceCulture);
            }
        }
    }
}