using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfApiproj.settings
{
    public interface ISettings
    {
        string baseAddress { get; set; }
       
    }

    public class Settings : ISettings
    {
        public string? baseAddress { get; set; }
       
    }
}
