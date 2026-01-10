---
id: 10
title: OpenAPI Specification Support
depends_on: [8, 9]
status: pending
---

# PBI-10: OpenAPI Specification Support

## Description

Enhance the API with comprehensive OpenAPI 3.0 documentation. The specification should include detailed endpoint descriptions, request/response examples, and proper schema definitions for all models including Problem Details and HATEOAS link structures. The OpenAPI spec must be available in all environments (not just development) and a static `openapi.json` file should be generated and committed for external tooling and CI validation.

## Acceptance Criteria

- [ ] XML documentation comments enabled in MazeOfHateoas.Api.csproj
- [ ] All controller actions have `<summary>` and `<remarks>` XML comments
- [ ] Request models have XML documentation on properties
- [ ] Response examples provided for each endpoint using Swashbuckle attributes
- [ ] Problem Details (RFC 7807) schema properly documented with examples
- [ ] HATEOAS Link model documented in OpenAPI schema
- [ ] Swagger UI available at `/swagger` in all environments (including production)
- [ ] Static `openapi.json` file generated and committed to repository root
- [ ] OpenAPI spec validates against OpenAPI 3.0 schema
- [ ] All response status codes documented (200, 201, 400, 404, 500)

## Technical Notes

### Current State
- Basic SwaggerGen configured in Program.cs
- Swagger UI only enabled in Development mode
- No XML comments or examples

### Implementation Hints
- Enable `<GenerateDocumentationFile>` in .csproj
- Use `[ProducesResponseType]` attributes for response documentation
- Use `[SwaggerOperation]` and `[SwaggerResponse]` for enhanced docs
- Add `SwaggerDoc` configuration with API info (title, version, description)
- Remove environment check for UseSwagger/UseSwaggerUI
- Use build task or script to generate static openapi.json

## Out of Scope
- API versioning
- Authentication/authorization documentation
- Rate limiting documentation
