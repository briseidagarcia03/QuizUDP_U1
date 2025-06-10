using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzServidor.Models.DTOs
{
    public class RespuestaDTO:Base
    {
        public string Usuario { get; set; } = null!;
        public int RespuestaUsuario { get; set; }
    }
}
