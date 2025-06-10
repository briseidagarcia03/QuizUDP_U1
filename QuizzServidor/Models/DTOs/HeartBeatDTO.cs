using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzServidor.Models.DTOs
{
    public class HeartBeatDTO
    {
        public string NombreUsuario { get; set; } = null!; // Nombre del usuario que envía el heartbeat
    }
}
