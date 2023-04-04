using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Model
{
    /// <summary>
    /// 网络服务API的Token
    /// </summary>
    public class ApiToken
    {
        public ApiToken()
        {
        }

        public ApiToken(string name, string token)
        {
            Name = name;
            Token = token;
        }

        /// <summary>
        /// 服务名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 服务密钥
        /// </summary>
        public string Token { get; set; }
    }
}
