using System;
using System.Collections.Generic;
using System.ComponentModel; // Necesario para BindingList
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;

namespace GeneracionInterfacesXML
{
    public partial class Form1 : Form
    {
        // Variable a nivel de clase para almacenar los datos de la tabla (requiere BindingList para auto-actualización)
        private BindingList<Usuario> listaUsuarios;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string xmlPath = "Interfaz.xml";

            try
            {
                XDocument xmlDoc = XDocument.Load(xmlPath);

                // Llamar a la función recursiva para crear controles
                foreach (XElement controlElement in xmlDoc.Descendants("Formulario").Elements("Control"))
                {
                    CrearControlesRecursivo(controlElement, this);
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Error: El archivo {xmlPath} no se encontró. Asegúrate de que se copia al directorio de salida.", "Error de Configuración");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al generar la interfaz: {ex.Message}", "Error de Lógica");
            }
        }

        // --- FUNCIONALIDAD DATOS DE USUARIO ---

        /// <summary>
        /// Inicializa y devuelve la lista de usuarios de ejemplo.
        /// </summary>
        private BindingList<Usuario> GenerarDatosUsuarios()
        {
            // Inicializa la lista que se usará en toda la clase
            listaUsuarios = new BindingList<Usuario>
            {
                new Usuario { Id = 1, Nombre = "Elena Ríos", Ciudad = "Madrid", Activo = true },
                new Usuario { Id = 2, Nombre = "Marco Pérez", Ciudad = "Barcelona", Activo = false },
                new Usuario { Id = 3, Nombre = "Laura Gómez", Ciudad = "Sevilla", Activo = true },
                new Usuario { Id = 4, Nombre = "Javier Cano", Ciudad = "Valencia", Activo = true }
            };
            return listaUsuarios;
        }

        // --- FUNCIÓN RECURSIVA PARA GENERACIÓN DE INTERFAZ ---

        /// <summary>
        /// Función recursiva para crear controles a partir del XML.
        /// </summary>
        private void CrearControlesRecursivo(XElement controlElement, Control contenedorPadre)
        {
            string tipo = controlElement.Attribute("Tipo")?.Value;
            string nombre = controlElement.Attribute("Nombre")?.Value;
            string texto = controlElement.Attribute("Texto")?.Value;

            // Propiedades de estilo desde XML
            string backColorName = controlElement.Attribute("BackColor")?.Value;
            string foreColorName = controlElement.Attribute("ForeColor")?.Value;
            string fontStyleName = controlElement.Attribute("FontStyle")?.Value;

            int.TryParse(controlElement.Attribute("PosicionX")?.Value, out int x);
            int.TryParse(controlElement.Attribute("PosicionY")?.Value, out int y);
            int.TryParse(controlElement.Attribute("Ancho")?.Value, out int ancho);
            int.TryParse(controlElement.Attribute("Alto")?.Value, out int alto);

            Control nuevoControl = null;

            if (tipo == "Label")
            {
                Label lbl = new Label();
                lbl.Text = texto;
                lbl.AutoSize = true;
                nuevoControl = lbl;
            }
            else if (tipo == "Button")
            {
                Button btn = new Button();
                btn.Text = texto;
                btn.Click += new EventHandler(btn_Click);
                nuevoControl = btn;
            }
            else if (tipo == "Panel")
            {
                Panel pnl = new Panel();
                if (!string.IsNullOrEmpty(backColorName) && backColorName != "None")
                {
                    try { pnl.BorderStyle = BorderStyle.FixedSingle; } catch { }
                }
                nuevoControl = pnl;
            }
            else if (tipo == "DataGridView")
            {
                DataGridView dgv = new DataGridView();
                dgv.RowHeadersVisible = false;
                dgv.AllowUserToAddRows = false;
                dgv.AutoGenerateColumns = true;

                // Asigna la fuente de datos generada
                dgv.DataSource = GenerarDatosUsuarios();

                nuevoControl = dgv;
            }

            if (nuevoControl != null)
            {
                nuevoControl.Name = nombre;
                nuevoControl.Location = new Point(x, y);

                // --- Aplicar estilos dinámicamente ---
                try
                {
                    if (!string.IsNullOrEmpty(backColorName))
                        nuevoControl.BackColor = Color.FromName(backColorName);

                    if (!string.IsNullOrEmpty(foreColorName))
                        nuevoControl.ForeColor = Color.FromName(foreColorName);

                    if (!string.IsNullOrEmpty(fontStyleName) && fontStyleName != "Regular")
                    {
                        FontStyle style = (FontStyle)Enum.Parse(typeof(FontStyle), fontStyleName);
                        nuevoControl.Font = new Font(nuevoControl.Font, style);
                    }
                }
                catch { /* Ignorar errores de color/fuente no válidos */ }

                // Aplicar tamaño
                if (nuevoControl is Panel || nuevoControl is Button || nuevoControl is DataGridView)
                {
                    nuevoControl.Size = new Size(ancho, alto);
                }
                else if (nuevoControl is Label)
                {
                    nuevoControl.Size = new Size(ancho, alto);
                }

                contenedorPadre.Controls.Add(nuevoControl);

                // Recursividad para contenedores
                if (nuevoControl is Panel)
                {
                    foreach (XElement childElement in controlElement.Elements("Control"))
                    {
                        CrearControlesRecursivo(childElement, nuevoControl);
                    }
                }
            }
        }

        // --- FUNCIONALIDAD DE BOTONES (GENERACIÓN Y MANIPULACIÓN DE DATOS) ---

        private void btn_Click(object sender, EventArgs e)
        {
            Button botonPresionado = sender as Button;

            if (botonPresionado != null)
            {
                // 1. Limpiamos los controles generados dinámicamente en la zona de respuesta
                LimpiarControlesDinamicos();

                // 2. Identificamos el botón y ejecutamos la lógica de manipulación
                switch (botonPresionado.Name)
                {
                    case "btnAnadir":
                        ManipularDatosAnadir();
                        break;
                    case "btnEditar":
                        ManipularDatosEditar();
                        break;
                    case "btnEliminar":
                        ManipularDatosEliminar();
                        break;
                    default:
                        MessageBox.Show($"Has pulsado el botón: {botonPresionado.Text}", "Evento de Botón");
                        break;
                }
            }
        }

        /// <summary>
        /// Elimina los controles generados dinámicamente en la zona de respuesta para evitar acumulación.
        /// </summary>
        private void LimpiarControlesDinamicos()
        {
            var controlesAEliminar = this.Controls.OfType<Control>()
                                        .Where(c => c.Location.Y >= 350)
                                        .ToList();

            foreach (var control in controlesAEliminar)
            {
                this.Controls.Remove(control);
                control.Dispose();
            }
        }

        /// <summary>
        /// Lógica del botón AÑADIR: Añade un nuevo usuario a la lista.
        /// </summary>
        private void ManipularDatosAnadir()
        {
            if (listaUsuarios != null)
            {
                int nuevoId = listaUsuarios.Any() ? listaUsuarios.Max(u => u.Id) + 1 : 1;

                listaUsuarios.Add(new Usuario
                {
                    Id = nuevoId,
                    Nombre = $"Usuario Añadido {nuevoId}",
                    Ciudad = "Generada",
                    Activo = true
                });

                // Feedback visual dinámico
                Label lbl = new Label();
                lbl.Text = $"? Usuario {nuevoId} añadido con éxito.";
                lbl.Location = new Point(50, 370);
                lbl.AutoSize = true;
                lbl.ForeColor = Color.DarkGreen;
                this.Controls.Add(lbl);
            }
        }

        /// <summary>
        /// Lógica del botón EDITAR: Modifica el primer usuario de la lista.
        /// </summary>
        private void ManipularDatosEditar()
        {
            var usuarioAEditar = listaUsuarios?.FirstOrDefault(u => u.Id == 1);

            if (usuarioAEditar != null)
            {
                usuarioAEditar.Nombre = "Elena (Editada)";
                usuarioAEditar.Ciudad = "París";
                listaUsuarios.ResetBindings(); // Forzar la actualización visual

                // Feedback visual dinámico
                Label lbl = new Label();
                lbl.Text = $"?? Usuario ID: 1 modificado a '{usuarioAEditar.Nombre}'";
                lbl.Location = new Point(50, 370);
                lbl.AutoSize = true;
                lbl.ForeColor = Color.DarkOrange;
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                this.Controls.Add(lbl);
            }
        }

        /// <summary>
        /// Lógica del botón ELIMINAR: Elimina el último usuario de la lista.
        /// </summary>
        private void ManipularDatosEliminar()
        {
            if (listaUsuarios != null && listaUsuarios.Any())
            {
                var ultimoUsuario = listaUsuarios.Last();
                listaUsuarios.Remove(ultimoUsuario);

                // Feedback visual dinámico
                Label lbl = new Label();
                lbl.Text = $"? Usuario {ultimoUsuario.Id} ('{ultimoUsuario.Nombre}') ELIMINADO.";
                lbl.Location = new Point(50, 370);
                lbl.AutoSize = true;
                lbl.ForeColor = Color.DarkRed;
                this.Controls.Add(lbl);
            }
            else
            {
                // Feedback visual si la lista está vacía
                Label lbl = new Label();
                lbl.Text = "La lista ya está vacía. No se puede eliminar.";
                lbl.Location = new Point(50, 370);
                lbl.AutoSize = true;
                lbl.ForeColor = Color.Red;
                this.Controls.Add(lbl);
            }
        }
    }
}