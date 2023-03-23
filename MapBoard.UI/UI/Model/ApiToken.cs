using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.UI.Model
{
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

        public string Name { get; set; }
        public string Token { get; set; }
    }
}
