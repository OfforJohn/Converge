# Token-Based Scope Extraction

## Overview

The Configuration API now extracts scope and tenant/company IDs from JWT Bearer tokens instead of requiring them in the request body. This improves security and simplifies the API.

## JWT Token Claims

Your JWT token should include the following custom claims:

```json
{
  "scope": "Global|Tenant|Company",
  "tenantId": "guid (optional, required for Tenant/Company scopes)",
  "companyId": "guid (optional, for Company scope)"
}
```

## API Usage

### Create Configuration

**Old Way (now simplified):**
```bash
POST /api/config
Content-Type: application/json
Authorization: Bearer {token}

{
  "key": "mykey",
  "value": "0.15",
  "scope": "Tenant",           # NO LONGER NEEDED
  "tenantId": "xxx",           # NO LONGER NEEDED
  "domain": "MyDomain"
}
```

**New Way:**
```bash
POST /api/config
Content-Type: application/json
Authorization: Bearer {token}

{
  "key": "mykey",
  "value": "0.15",
  "domain": "MyDomain"
}
```

The API extracts `scope` and `tenantId` from your Bearer token automatically.

### Get Configuration

**With Tenant Scope:**
```bash
GET /api/config/mykey
Authorization: Bearer {token}
```

Token contains:
```json
{
  "scope": "Tenant",
  "tenantId": "2fbaff7f-14a3-4bbb-bd23-f104d8370b43"
}
```

**With Company Scope:**
```bash
GET /api/config/mykey
Authorization: Bearer {token}
```

Token contains:
```json
{
  "scope": "Company",
  "tenantId": "2fbaff7f-14a3-4bbb-bd23-f104d8370b43",
  "companyId": "3cb87a7f-25b4-5ccc-ce34-g215e9481c54"
}
```

**With Global Scope:**
```bash
GET /api/config/mykey
Authorization: Bearer {token}
```

Token contains:
```json
{
  "scope": "Global"
}
```

### Update Configuration

**Old Way (now simplified):**
```bash
PUT /api/config/mykey
Content-Type: application/json
Authorization: Bearer {token}

{
  "value": "0.20",
  "expectedVersion": 5,
  "scope": "Tenant",           # NO LONGER NEEDED
  "tenantId": "xxx"            # NO LONGER NEEDED
}
```

**New Way:**
```bash
PUT /api/config/mykey
Content-Type: application/json
Authorization: Bearer {token}

{
  "value": "0.20",
  "expectedVersion": 5
}
```

## Example JWT Token Creation (for Testing)

Here's a sample JWT token structure:

```csharp
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes("your-secret-key");

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim("scope", "Tenant"),
        new Claim("tenantId", "2fbaff7f-14a3-4bbb-bd23-f104d8370b43"),
        new Claim(ClaimTypes.Name, "user@example.com")
    }),
    Expires = DateTime.UtcNow.AddHours(1),
    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
};

var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);
```

## Supported Token Claim Names

The API looks for claims in this order:

1. **Scope**: `scope` ? `role` ? defaults to `Global`
2. **TenantId**: `tenantId` 
3. **CompanyId**: `companyId`

## Behavior

- If no Bearer token is provided, defaults to `Global` scope
- If `scope` claim is missing or invalid, defaults to `Global`
- If `tenantId` is required but missing, the service will return validation errors
- All three scopes (`Global`, `Tenant`, `Company`) are now supported seamlessly
