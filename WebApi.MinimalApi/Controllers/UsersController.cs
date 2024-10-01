﻿using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var possibleUser = userRepository.FindById(userId);

        if (possibleUser == null)
            return NotFound();

        var userDto = mapper.Map<UserDto>(possibleUser);
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();
        if (user.Login == null)
        {

            return UnprocessableEntity(ModelState);
        }

        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login must contain only characters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));

        bool isInsert;
        userRepository.UpdateOrInsert(userEntity, out isInsert);

        if (isInsert)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId },
                userId);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();
        
        var updateUserDto = mapper.Map<UpdateUserDto>(user);

        patchDoc.ApplyTo(updateUserDto, ModelState);

        TryValidateModel(updateUserDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        return NoContent();
    }

}