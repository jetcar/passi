﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using passi_webapi.Dto;
using passi_webapi.Filters;
using Repos;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {
        private PassiDbContext _db;
        private IMapper _mapper;
        private static int pagesize = 100;

        public AdminController(PassiDbContext db, IMapper mapper)
        {
            this._db = db;
            _mapper = mapper;
        }

        [HttpGet, Route("Login")]
        public async Task Login(string returnUrl = "/passiapi")
        {
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }

            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = returnUrl,
            });
        }

        [HttpGet, Route("Logout")]
        public async Task<RedirectResult> Logout()
        {
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }

            return Redirect("/");
            //await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet, Route("GetUsers")]
        [Authorization]
        public async Task<List<UserDto>> GetUsers(int page = 0)
        {
            return _db.Users.Skip(page * page).Take(pagesize).Select(_mapper.Map<UserDto>).ToList();
        }
    }
}