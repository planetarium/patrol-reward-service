using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;

namespace PatrolRewardService;

internal static class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
            Console.WriteLine("Migrate db.");
            db.Database.Migrate();
        }

        host.Run();
    }

    // EF Core uses this method at design time to access the DbContext
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                IConfiguration configRoot = config.Build();
                GraphqlClientOptions graphqlClientOptions = new();
                configRoot.GetSection(GraphqlClientOptions.GraphqlClientConfig)
                    .Bind(graphqlClientOptions);
                SignerOptions signerOptions = new();
                configRoot.GetSection(SignerOptions.SignerConfig)
                    .Bind(signerOptions);
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<StartUp>());
    }
}

public class StartUp
{
    public StartUp(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add services to the container.
        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Database
        services.AddDbContextFactory<RewardDbContext>(options =>
        {
            options
                .UseNpgsql(Configuration.GetConnectionString("PatrolReward"))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
        });

        // GraphqlClient
        services
            .AddSingleton<NineChroniclesClient>(sp =>
            {
                var configRoot = sp.GetRequiredService<IConfiguration>();
                GraphqlClientOptions graphqlClientOptions = new();
                configRoot.GetSection(GraphqlClientOptions.GraphqlClientConfig)
                    .Bind(graphqlClientOptions);
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(graphqlClientOptions),
                    loggerFactory);
            });

        // Graphql
        services
            .AddGraphQLServer()
            .RegisterService<NineChroniclesClient>()
            .RegisterService<Signer>()
            .RegisterDbContext<RewardDbContext>(DbContextKind.Pooled)
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddErrorFilter<GraphqlErrorFilter>();

        // Signer
        services.AddSingleton<Signer>(_ =>
        {
            SignerOptions signerOptions = new();
            Configuration.GetSection(SignerOptions.SignerConfig)
                .Bind(signerOptions);
            return new Signer(new OptionsWrapper<SignerOptions>(signerOptions));
        });

        // Worker
        services.AddHostedService<TransactionWorker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
            endpoints.MapControllers();
        });
    }

    public class GraphqlErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithMessage(error.Message);
        }
    }
}
