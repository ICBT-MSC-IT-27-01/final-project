namespace AnuradhapuraAI.Api;

internal static class HealthDashboardPage
{
    public const string ContentType = "text/html; charset=utf-8";

    public static string Render(string serviceName, string? modelVersion = null) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{serviceName}} Health</title>
          <style>
            body { align-items: center; background: #f0f9ff; color: #172554; display: flex; font-family: Arial, sans-serif; justify-content: center; margin: 0; min-height: 100vh; }
            main { background: white; border: 1px solid #bae6fd; border-radius: 14px; box-shadow: 0 10px 25px #0c4a6e22; max-width: 560px; padding: 32px; width: calc(100% - 32px); }
            h1 { color: #075985; margin-top: 0; } .status { border-radius: 8px; font-weight: bold; margin: 18px 0; padding: 14px; }
            .checking { background: #fef3c7; color: #92400e; } .healthy { background: #dcfce7; color: #166534; } .unhealthy { background: #fee2e2; color: #991b1b; }
            code { background: #eff6ff; border-radius: 4px; padding: 2px 5px; } .detail { color: #475569; } a { color: #0369a1; }
          </style>
        </head>
        <body>
          <main>
            <h1>{{serviceName}}</h1>
            <p class="detail">Read-only local service health dashboard.</p>
            <div id="status" class="status checking">Checking <code>/health</code>…</div>
            <p id="details" class="detail">This page checks service availability only. It does not display database, credential or configuration details.</p>
            <p><a href="/health">Open the JSON health response</a></p>
          </main>
          <script>
            const status = document.getElementById('status');
            const details = document.getElementById('details');
            async function checkHealth() {
              try {
                const response = await fetch('/health', { cache: 'no-store' });
                const body = await response.json();
                if (!response.ok || body.status !== 'Healthy') throw new Error('Unhealthy response');
                status.className = 'status healthy';
                status.textContent = 'Healthy';
                details.textContent = `Last checked: ${new Date().toLocaleString()}${body.modelVersion ? ` · Model: ${body.modelVersion}` : ''}`;
              } catch {
                status.className = 'status unhealthy';
                status.textContent = 'Unavailable';
                details.textContent = 'The /health endpoint could not be reached. Confirm that this local service is running.';
              }
            }
            checkHealth();
            setInterval(checkHealth, 30000);
          </script>
        </body>
        </html>
        """;
}
