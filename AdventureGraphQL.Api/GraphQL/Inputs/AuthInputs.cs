namespace AdventureGraphQL.Api.GraphQL;

/// <summary>Lo que envía el cliente para iniciar sesión.</summary>
public record LoginInput(string Username, string Password);

/// <summary>Lo que devuelve el login: el token con el que el cliente "ingresa".</summary>
public record AuthPayload(string Token, DateTime ExpiresAt, string Username);
