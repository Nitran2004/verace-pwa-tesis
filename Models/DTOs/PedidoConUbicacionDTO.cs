﻿using ProyectoIdentity.Models;
namespace ProyectoIdentity.Models.DTOs

{
    public class PedidoConUbicacionDTO
    {
        public List<int> ProductosIdsSeleccionados { get; set; } = new List<int>();
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }
}
