﻿using AutoMapper;
using MagicVilla_API.Datos;
using MagicVilla_API.Modelos;
using MagicVilla_API.Modelos.Dto;
using MagicVilla_API.Repositorio.Repositorio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MagicVilla_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaController : ControllerBase
    {
        // Inyectamos ILoger para usar sus mensajes de informacion en los endpoints
        private readonly ILogger<VillaController> _logger;
        private readonly IVillaRepositorio _villaRepositorio;
        // Despues de lo anterior inyectamos el mapeo
        private readonly IMapper _mapper;
        // Importante nuestra variable de respuesta
        protected ApiResponse _response;
        public VillaController(ILogger<VillaController> logger, IVillaRepositorio villaRepositorio, IMapper mapper)
        {
            _logger = logger;
            _villaRepositorio= villaRepositorio;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse>> GetVillas()
        {
            try
            {

                _logger.LogInformation("Obtener todas las villas");

                IEnumerable<Villa> villaList = await _villaRepositorio.ObtenerTodos();

                _response.Resultado = _mapper.Map<IEnumerable<VillaDto>>(villaList);
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {

                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return _response;
        }

        [HttpGet("id:int", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> GetVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogError("Error al traer villa con ID:" + id); // Esta es una de las tantas formas de usar ILogger
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsExistoso = false;
                    return BadRequest(_response);
                }

                // var villa = VillaStore.villaList.FirstOrDefault(v => v.Id == id);
                var villa = await _villaRepositorio.Obtener(x => x.Id == id);
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsExistoso = false;
                    return NotFound(_response);
                }
                _response.Resultado = _mapper.Map<VillaDto>(villa);
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }

            return _response;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> CrearVilla([FromBody] VillaCreateDto createDto)
        {
            try
            {   // Validaciones ModelState
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validaciones personalizadas
                if (await _villaRepositorio.Obtener(v => v.Nombre.ToLower() == createDto.Nombre.ToLower()) != null)
                {
                    ModelState.AddModelError("NombreExiste", "La villa con ese nombre ya existe");
                    return BadRequest(ModelState);
                }

                if (createDto is null)
                    return BadRequest();

                //villaDto.Id = VillaStore.villaList.OrderByDescending(v => v.Id).FirstOrDefault().Id + 1;
                //VillaStore.villaList.Add(villaDto); Estas dos lineas no se necesitaran

                // Todo esto se reemplaza por el automapper
                //Villa modelo = new()
                //{
                //    Nombre = villaDto.Nombre,
                //    Detalle = villaDto.Detalle,
                //    ImagenUrl = villaDto.ImagenUrl,
                //    Ocupantes = villaDto.Ocupantes,
                //    Tarifa = villaDto.Tarifa,
                //    MetrosCuadrados = villaDto.MetrosCuadrados,
                //    Amenidad = villaDto.Amenidad
                //};

                Villa modelo = _mapper.Map<Villa>(createDto);

                modelo.FechaCreacion = DateTime.Now;
                modelo.FechaActualizacion = DateTime.Now;

                await _villaRepositorio.Crear(modelo);
                _response.Resultado = modelo;
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetVilla", new { id = modelo.Id }, _response);

            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }

            return _response;
        }

        [HttpDelete("id:int")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var villa = await _villaRepositorio.Obtener(x => x.Id == id);
                if (villa is null)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                await _villaRepositorio.Remover(villa);
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }

            return BadRequest(_response);
        }

        //[HttpPut("id:int")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public IActionResult UpdateVilla(int id, [FromBody] VillaUpdateDto villaDto)
        //{
        //    if (villaDto == null || id != villaDto.Id)
        //        return BadRequest();

        //    //var villa = VillaStore.villaList.FirstOrDefault(x => x.Id == id);
        //    //villa.Nombre = villaDto.Nombre;
        //    //villa.Ocupantes = villaDto.Ocupantes;
        //    //villa.MetrosCuadrados = villaDto.MetrosCuadrados;  sOLO REEMPLKAZA EN EL VILLASTORE

        //    Villa modelo = new()
        //    {
        //        Id = villaDto.Id,
        //        Nombre = villaDto.Nombre,
        //        Detalle = villaDto.Detalle,
        //        ImagenUrl = villaDto.ImagenUrl,
        //        Ocupantes = villaDto.Ocupantes,
        //        Tarifa = villaDto.Tarifa,
        //        MetrosCuadrados = villaDto.MetrosCuadrados,
        //        Amenidad = villaDto.Amenidad
        //    };

        //    _context.Villas.Update(modelo);
        //    _context.SaveChanges();

        //    return NoContent();
        //}

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateVilla(int id, [FromBody] VillaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null || id != updateDto.Id)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                //var modelo = await _context.Villas.FirstOrDefaultAsync(v => v.Id == id);

                //if (modelo == null)
                //    return NotFound();

                // Se reemplaza por el mapper
                //modelo.Nombre = villaDto.Nombre;
                //modelo.Detalle = villaDto.Detalle;
                //modelo.ImagenUrl = villaDto.ImagenUrl;
                //modelo.Ocupantes = villaDto.Ocupantes;
                //modelo.Tarifa = villaDto.Tarifa;
                //modelo.MetrosCuadrados = villaDto.MetrosCuadrados;
                //modelo.Amenidad = villaDto.Amenidad;

                Villa modelo = _mapper.Map<Villa>(updateDto);
                await _villaRepositorio.Actualizar(modelo);

                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return BadRequest(_response);
        }

        // HttpPatch en accion, usado solamente para cambniar una sola propiedad de un modelo
        [HttpPatch("id:int")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDto> patchDto)
        {
            try
            {
                if (patchDto == null || id == 0)
                    return BadRequest();

                //var villa = VillaStore.villaList.FirstOrDefault(x => x.Id == id);
                var villa = await _villaRepositorio.Obtener(v => v.Id == id, tracked: false);

                // Se reemplaza por mapper
                //VillaUpdateDto villaDto = new()
                //{
                //    Id = villa.Id,
                //    Nombre = villa.Nombre,
                //    Detalle = villa.Detalle,
                //    ImagenUrl = villa.ImagenUrl,
                //    Ocupantes = villa.Ocupantes,
                //    Tarifa = villa.Tarifa,
                //    MetrosCuadrados = villa.MetrosCuadrados,
                //    Amenidad = villa.Amenidad
                //};

                VillaUpdateDto villaDto = _mapper.Map<VillaUpdateDto>(villa);

                if (villa == null)
                    return NotFound();


                patchDto.ApplyTo(villaDto, ModelState);

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Igual se reemplaza por mapper
                //Villa modelo = new()
                //{
                //    Id = villaDto.Id,
                //    Nombre = villaDto.Nombre,
                //    Detalle = villaDto.Detalle,
                //    ImagenUrl = villaDto.ImagenUrl,
                //    Ocupantes = villaDto.Ocupantes,
                //    Tarifa = villaDto.Tarifa,
                //    MetrosCuadrados = villaDto.MetrosCuadrados,
                //    Amenidad = villaDto.Amenidad
                //};

                Villa modelo = _mapper.Map<Villa>(villaDto);

                await _villaRepositorio.Actualizar(modelo);
                _response.StatusCode = HttpStatusCode.NoContent;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return BadRequest(_response);
        }
    }
}
