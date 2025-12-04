namespace GestorRRHH
{
    public static class SesionActual
    {
        public static int IdUsuario { get; set; }
        public static string NombreUsuario { get; set; }
        public static string Rol { get; set; } // Admin o Empleado

        // Método para cerrar sesión correctamente
        public static void CerrarSesion()
        {
            IdUsuario = 0;
            NombreUsuario = null;
            Rol = null;
        }
    }
}
 