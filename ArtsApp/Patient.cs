using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtsApp
{
    class Patient
    {
        public List<String> chathistory { get; }
        public String username { get; set; }
        public String currentBikeData { get; set; }

        public Patient(string username)
        {
            this.username = username;
            currentBikeData = "";
        }
    }
}
