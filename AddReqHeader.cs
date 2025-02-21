using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Apply header only for "/secured-endpoint"
        if (context.ApiDescription.RelativePath.Contains("gateway"))
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "BaseUrl",
                In = ParameterLocation.Header,
                Required = true, // 🔥 Mandatory header for this endpoint
                Description = "Require base URL for this endpoint"
            });
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Model",
                In = ParameterLocation.Header,
                Required = true, // 🔥 Mandatory header for this endpoint
                Description = "Require URL for this endpoint"
            });
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Key",
                In = ParameterLocation.Header,
                Required = true, // 🔥 Mandatory header for this endpoint
                Description = "Require URL for this endpoint"
            });
            //define request body with json
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["text"] = new OpenApiSchema
                                {
                                    Type = "string"
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}