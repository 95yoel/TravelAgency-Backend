﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsturTravel.Data;
using AsturTravel.Models;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Cors;

namespace AsturTravel.Controllers
{
    [EnableCors("AllowAllOrigins")]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        //CARGAR VISTA PARCIAL DE CREAR RESERVAS
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Nombre");
            ViewData["ViajeId"] = new SelectList(_context.Viajes, "Id", "Nombre");
            return PartialView("PartialsHomeAdmin/Reservas/_PartialCreateReservas");
        }

        //CREAR RESERVAS DESDE EL BACKEND
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservas reservas)
        {
            if (ModelState.IsValid)
            {

                var precioString = reservas.PrecioString;
                reservas.Precio = Math.Round(double.Parse(precioString, CultureInfo.InvariantCulture),2);
                reservas.FechaModificacion = DateTime.Now;
                reservas.FechaReserva = DateTime.Now;

                _context.Add(reservas);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Id", reservas.UsuarioId);
            ViewData["ViajeId"] = new SelectList(_context.Viajes, "Id", "Id", reservas.ViajeId);
            return RedirectToAction("Index", "Home");
        }

        //CREAR RESERVAS DESDE EL FRONTEND
        [HttpPost]
        public async Task<IActionResult> Crear()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var requestBody = await reader.ReadToEndAsync();

                var datosReserva = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);

                var idUsuario = int.Parse(datosReserva["id"]);
                var idViaje = datosReserva["viaje"];
                var numeroPersonas = datosReserva["numPersonas"];
                var precio = datosReserva["precio"];
                var precioDecimal = double.Parse(precio.Replace(",", "."));
                var precioRedondeado = Math.Round(precioDecimal, 2);

                var culture = new CultureInfo("es-ES");
               

                var reservas = new Reservas
                {
                    Precio = Math.Round(double.Parse(precio, culture), 2),
                    FechaModificacion = DateTime.Now,
                    FechaReserva = DateTime.Now,
                    NumeroPersonas = int.Parse(numeroPersonas),
                    ViajeId = int.Parse(idViaje),
                    UsuarioId = idUsuario,
                    FechaPago = DateTime.Now
                };

                _context.Add(reservas);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
        }


        // CARGAR VISTA EDITAR RESERVAS
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Reservas == null)
            {
                return NotFound();
            }

            var reservas = await _context.Reservas.FindAsync(id);
            if (reservas == null)
            {
                return NotFound();
            }
            ViewBag.ReservasData = JsonConvert.SerializeObject(new { Id = reservas.Id, ViajeId = reservas.ViajeId });
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "NombreCompleto", reservas.UsuarioId);
            ViewData["ViajeId"] = new SelectList(_context.Viajes, "Id", "Nombre", reservas.ViajeId);
            return PartialView("PartialsHomeAdmin/Reservas/_PartialsEditarReservas");
        }

        //EDITAR RESERVAS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Reservas reservas)
        {
            if (id != reservas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var precioString = reservas.PrecioString;
                    reservas.Precio = Math.Round(double.Parse(precioString),2);

                    reservas.FechaModificacion = DateTime.Now;


                    _context.Update(reservas);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservasExists(reservas.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Home");
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "NombreCompleto", reservas.UsuarioId);
            ViewData["ViajeId"] = new SelectList(_context.Viajes, "Id", "Nombre", reservas.ViajeId);
            return RedirectToAction("Index", "Home");
        }

        //CANCELAR RESERVA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva == null)
            {
                return NotFound();
            }

            reserva.FechaCancelacion = DateTime.Now;
            reserva.FechaModificacion = DateTime.Now;

            _context.Update(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        //BORRAR RESERVA
        public async Task<IActionResult> Delete2(int id)
        {
            if (_context.Reservas == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Reservas'  is null.");
            }
            var reservas = await _context.Reservas.FindAsync(id);
            if (reservas != null)
            {
                _context.Reservas.Remove(reservas);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        private bool ReservasExists(int id)
        {
          return _context.Reservas.Any(e => e.Id == id);
        }

        //OBTENER JSON DE RESERVAS
        [HttpGet("reserva/GetJson")]
        public IActionResult GetJson()
        {
            List<Reservas> listaReservas = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.Destino)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.TipoViaje)
       
                .ToList();
            return Json(listaReservas);
        }

        //OBTENER RESERVAS QUE TIENE UN USUARIO 
        [HttpGet("reserva/reservasUsuario/{id}")]
        public async Task<ActionResult<IEnumerable<Reservas>>> GetReservasByUsuario(int id)
        {
            var reservas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.Destino)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.TipoViaje)
                .Where(r => r.UsuarioId == id)
                .ToListAsync();

            if (reservas == null || reservas.Count == 0)
            {
                return Json(new List<Reservas>());
            }

            return reservas;
        }


        //OBTENER INFORMACION DE UNA RESERVA EN CONCRETO

        [HttpGet("reserva/infoReserva/{id}")]
        public async Task<ActionResult<Reservas>> GetReservaById(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.Destino)
                .Include(r => r.Viaje)
                    .ThenInclude(v => v.TipoViaje)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
            {
                return Json(new List<Reservas>());
            }

            return reserva;
        }

        //OBTENER EL PRECIO DE UN VIAJE SEGÚN SU ID
        public IActionResult getPrecioViaje(int id)
        {
            var viaje = _context.Viajes.Find(id);
            if(id == -1)
            {
                return Json(0);
            }
            else
            {
                return Json(viaje.Precio);
            }
            
        }

        //CALCULAR EL PRECIO DE UN VIAJE SEGÚN SU ID Y EL NUMERO DE PASAJEROS
        public IActionResult CalcularPrecioViaje(int id,int num)
        {
            var viaje = _context.Viajes.Find(id);
            if (id == -1)
            {
                return Json(0);
            }
            else
            {
                var precio = viaje.Precio * num;
                return Json(precio);
            }

        }

        //OBTENER EL PRECIO DE UNA RESERVA SEGÚN SU ID
        public IActionResult GetPrecio(int id)
        {

            var precio = _context.Reservas.Find(id).Precio;
            

            return Json(new { precio });
        }

        //OBTENER LOS DATOS DE UN CLIENTE SEGÚN SU ID
        public IActionResult getDatosCliente(int id)
        {
           
           if(id != -1)
            {
                var usuario = _context.Usuario.Find(id);

                var getDatosCliente = new
                {
                    nombre = usuario.Nombre,
                    apeliidos = usuario.Apellidos,
                    email = usuario.Email,
                    telefono = usuario.Telefono,
                    dni = usuario.DNI,
                    codpost = usuario.CODPOST

                };

                return Json(getDatosCliente);
            }
            else
            {
                var getDatosCliente = new
                {
                    nombre = "",
                    apeliidos = "",
                    email = "",
                    telefono = "",
                    dni = "",
                    codpost = ""
                };
                return Json(getDatosCliente);
            }
        }

        //CARGAR VISTA PARCIAL INDEX DE RESERVAS 
        public IActionResult PartialIndex()
        {
            var reservas = _context.Reservas.Include(r => r.Usuario).Include(r => r.Viaje);

            return PartialView("PartialsHomeAdmin/Reservas/_PartialReservas", reservas);
        }






    }
}
