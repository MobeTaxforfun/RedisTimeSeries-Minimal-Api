using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

var redisserver = configuration.GetValue<string>("redisserver");
builder.Services.AddSingleton<IDatabase>(ConnectionMultiplexer.Connect(redisserver).GetDatabase(0));
var app = builder.Build();

app.MapGet("/hello", () => "HI");

app.MapPost("/AddBrowseLog", async (HttpContext ctx, IDatabase redisDb) =>
{
    await redisDb.ExecuteAsync(
        "TS.ADD",
        ctx.Request.Form["BL_UID"].ToString(),
        new DateTimeOffset(Convert.ToDateTime(ctx.Request.Form["BL_TIME"]).Date).ToUnixTimeSeconds(),
        1,
        "ON_DUPLICATE",
        "SUM"
        );
    return Results.Json(new { Status = true, Message = "成功" });
});

app.MapPost("/GetBrowseLog", async (HttpContext ctx, IDatabase redisDb) =>
{
    string bl_uid = ctx.Request.Form["BL_UID"].ToString();

    try
    {
        var result = await redisDb.ExecuteAsync(
            "TS.RANGE",
            ctx.Request.Form["BL_UID"].ToString(),
            new DateTimeOffset(Convert.ToDateTime(ctx.Request.Form["BL_TIME_S"])).ToUnixTimeSeconds(),
            new DateTimeOffset(Convert.ToDateTime(ctx.Request.Form["BL_TIME_E"])).ToUnixTimeSeconds()
            );

        var temp = ((RedisResult[])result);
        List<Deepsleep> temp2 = new List<Deepsleep>();

        foreach (var item in temp)
        {
            temp2.AddRange(item.ToDictionary()
                .Select(c => new Deepsleep
                {
                    BL_UID = bl_uid,
                    TIME = new DateTime(1970, 1, 1).AddTicks((Convert.ToInt64(c.Key) + 8 * 60 * 60) * 10000000).ToString("yyyy-MM-dd"),
                    count = ((int)c.Value)
                }));
        }

        return Results.Json(new { Status = true, Message = "成功", Data = temp2 });

    }
    catch (Exception ex)
    {
        return Results.Json(new { Status = true, Message = "失敗，無相關資料" });
    }

});

app.Run();

class Deepsleep
{
    public string BL_UID { get; set; }
    public string TIME { get; set; }
    public int count { get; set; }
}

