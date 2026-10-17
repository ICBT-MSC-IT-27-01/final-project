from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm
from reportlab.platypus import (
    Flowable,
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


OUTPUT = Path(__file__).with_name("PROJECT_MODEL_API_ANGULAR_SQL_INTEGRATION_GUIDE.pdf")


class ArchitectureDiagram(Flowable):
    def __init__(self):
        super().__init__()
        self.width = 17.2 * cm
        self.height = 8.8 * cm

    def draw(self):
        canvas = self.canv
        boxes = [
            (0.1 * cm, 5.8 * cm, 3.3 * cm, 1.45 * cm, "Angular", "Dashboard, recommendations, admin"),
            (5.0 * cm, 5.8 * cm, 3.8 * cm, 1.45 * cm, "ASP.NET Core API", "Controllers and application services"),
            (10.5 * cm, 5.8 * cm, 3.5 * cm, 1.45 * cm, "Python FastAPI", "LSTM v1 inference service"),
            (13.7 * cm, 1.4 * cm, 3.3 * cm, 1.45 * cm, "SQL Server", "Forecasts, users, configuration"),
            (5.0 * cm, 1.4 * cm, 3.8 * cm, 1.45 * cm, "Open-Meteo", "30 daily observations"),
            (10.4 * cm, 1.4 * cm, 3.5 * cm, 1.45 * cm, "LSTM artifacts", "v1.keras and scaler"),
        ]
        for x, y, width, height, title, subtitle in boxes:
            canvas.setFillColor(colors.HexColor("#EFF6FF"))
            canvas.setStrokeColor(colors.HexColor("#2563EB"))
            canvas.roundRect(x, y, width, height, 7, fill=1, stroke=1)
            canvas.setFillColor(colors.HexColor("#12345B"))
            canvas.setFont("Helvetica-Bold", 9)
            canvas.drawCentredString(x + width / 2, y + height - 0.48 * cm, title)
            canvas.setFont("Helvetica", 7.5)
            canvas.drawCentredString(x + width / 2, y + 0.35 * cm, subtitle)

        def arrow(x1, y1, x2, y2, label):
            canvas.setStrokeColor(colors.HexColor("#475569"))
            canvas.setFillColor(colors.HexColor("#475569"))
            canvas.setLineWidth(1.2)
            canvas.line(x1, y1, x2, y2)
            canvas.saveState()
            canvas.translate(x2, y2)
            # Simple downward/right-facing marker is sufficient for directional flow.
            canvas.circle(0, 0, 2.2, fill=1, stroke=0)
            canvas.restoreState()
            canvas.setFont("Helvetica", 7)
            canvas.drawCentredString((x1 + x2) / 2, (y1 + y2) / 2 + 7, label)

        arrow(3.4 * cm, 6.52 * cm, 5.0 * cm, 6.52 * cm, "HTTPS /api")
        arrow(8.8 * cm, 6.52 * cm, 10.5 * cm, 6.52 * cm, "HTTP /forecast")
        arrow(6.9 * cm, 5.8 * cm, 6.9 * cm, 2.85 * cm, "read/write")
        arrow(8.8 * cm, 2.12 * cm, 10.4 * cm, 2.12 * cm, "30-day input")
        arrow(12.15 * cm, 5.8 * cm, 12.15 * cm, 2.85 * cm, "load model + scaler")
        arrow(8.8 * cm, 6.15 * cm, 13.7 * cm, 2.65 * cm, "persist seven forecasts")
        canvas.setFillColor(colors.HexColor("#334155"))
        canvas.setFont("Helvetica-Oblique", 7.5)
        canvas.drawString(0.1 * cm, 0.35 * cm, "No database credentials travel to Angular or the Python service. SQL access remains inside ASP.NET Core.")


class SequenceDiagram(Flowable):
    def __init__(self):
        super().__init__()
        self.width = 17.2 * cm
        self.height = 9.3 * cm

    def draw(self):
        canvas = self.canv
        actors = [(1.3, "Administrator"), (5.1, "ASP.NET Core"), (8.7, "Open-Meteo"), (12.2, "FastAPI / LSTM"), (15.7, "SQL Server")]
        for x, title in actors:
            xpos = x * cm
            canvas.setFillColor(colors.HexColor("#E0F2FE"))
            canvas.setStrokeColor(colors.HexColor("#0284C7"))
            canvas.roundRect(xpos - 1.35 * cm, 8.15 * cm, 2.7 * cm, 0.65 * cm, 5, fill=1, stroke=1)
            canvas.setFillColor(colors.HexColor("#0C4A6E"))
            canvas.setFont("Helvetica-Bold", 7.3)
            canvas.drawCentredString(xpos, 8.4 * cm, title)
            canvas.setStrokeColor(colors.HexColor("#94A3B8"))
            canvas.setDash(2, 2)
            canvas.line(xpos, 0.3 * cm, xpos, 8.15 * cm)
            canvas.setDash()

        def message(y, start, end, text, response=False):
            x1, x2 = start * cm, end * cm
            canvas.setStrokeColor(colors.HexColor("#0F766E") if response else colors.HexColor("#334155"))
            canvas.setLineWidth(1)
            canvas.line(x1, y * cm, x2, y * cm)
            canvas.setFillColor(colors.HexColor("#0F766E") if response else colors.HexColor("#334155"))
            canvas.circle(x2, y * cm, 1.8, fill=1, stroke=0)
            canvas.setFont("Helvetica", 7.1)
            canvas.drawCentredString((x1 + x2) / 2, y * cm + 5, text)

        message(7.35, 1.3, 5.1, "POST /api/admin/forecasts/operational")
        message(6.45, 5.1, 8.7, "request last 30 complete local days")
        message(5.55, 8.7, 5.1, "daily rainfall, mean temperature, mean humidity", True)
        message(4.65, 5.1, 12.2, "POST /forecast with 30 validated observations")
        message(3.75, 12.2, 5.1, "seven dated v1 predictions", True)
        message(2.85, 5.1, 15.7, "save one Operational ForecastRun + seven ForecastRecords")
        message(1.95, 15.7, 5.1, "commit succeeds", True)
        message(1.05, 5.1, 1.3, "200 response with run summary", True)
        canvas.setFillColor(colors.HexColor("#334155"))
        canvas.setFont("Helvetica-Oblique", 7.2)
        canvas.drawString(0.2 * cm, 0.32 * cm, "The public dashboard later requests GET /api/forecasts/latest; it reads the persisted eligible operational run.")


def page_number(canvas, document):
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#CBD5E1"))
    canvas.line(1.7 * cm, 1.45 * cm, A4[0] - 1.7 * cm, 1.45 * cm)
    canvas.setFillColor(colors.HexColor("#64748B"))
    canvas.setFont("Helvetica", 8)
    canvas.drawString(1.7 * cm, 0.95 * cm, "AI-Driven Weather Forecasting and Crop Recommendation System — Integration Guide")
    canvas.drawRightString(A4[0] - 1.7 * cm, 0.95 * cm, f"Page {document.page}")
    canvas.restoreState()


def build_styles():
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(name="GuideTitle", parent=styles["Title"], fontName="Helvetica-Bold", fontSize=23, leading=28, textColor=colors.HexColor("#0F172A"), alignment=TA_CENTER, spaceAfter=14))
    styles.add(ParagraphStyle(name="GuideSubtitle", parent=styles["Normal"], fontSize=11, leading=16, textColor=colors.HexColor("#475569"), alignment=TA_CENTER, spaceAfter=22))
    styles.add(ParagraphStyle(name="GuideHeading", parent=styles["Heading2"], fontName="Helvetica-Bold", fontSize=15, leading=19, textColor=colors.HexColor("#0F4C5C"), spaceBefore=12, spaceAfter=8))
    styles.add(ParagraphStyle(name="GuideBody", parent=styles["BodyText"], fontSize=9.4, leading=14, textColor=colors.HexColor("#1E293B"), spaceAfter=7))
    styles.add(ParagraphStyle(name="GuideSmall", parent=styles["BodyText"], fontSize=8.2, leading=11.5, textColor=colors.HexColor("#334155")))
    styles.add(ParagraphStyle(name="Callout", parent=styles["BodyText"], fontSize=9.2, leading=13.5, textColor=colors.HexColor("#0F172A"), backColor=colors.HexColor("#ECFEFF"), borderColor=colors.HexColor("#0891B2"), borderWidth=0.5, borderPadding=8, spaceBefore=5, spaceAfter=10))
    return styles


def table(rows, widths, styles):
    formatted = []
    for row_index, row in enumerate(rows):
        formatted.append([Paragraph(str(value), styles["GuideSmall"]) for value in row])
    result = Table(formatted, colWidths=widths, repeatRows=1, hAlign="LEFT")
    result.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#0F4C5C")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#CBD5E1")),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F8FAFC")]),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    return result


def paragraph_list(items, styles):
    return [Paragraph(f"• {item}", styles["GuideBody"]) for item in items]


def build_document():
    styles = build_styles()
    document = SimpleDocTemplate(str(OUTPUT), pagesize=A4, rightMargin=1.7 * cm, leftMargin=1.7 * cm, topMargin=1.65 * cm, bottomMargin=1.85 * cm, title="Project Model, API, Angular and SQL Integration Guide", author="Anuradhapura AI Project")
    story = []
    story.extend([
        Spacer(1, 1.2 * cm),
        Paragraph("PROJECT MODEL, API, ANGULAR & SQL", styles["GuideTitle"]),
        Paragraph("Integration Guide", styles["GuideTitle"]),
        Paragraph("A beginner-friendly explanation of the working MSc prototype for Anuradhapura District", styles["GuideSubtitle"]),
        Paragraph("Purpose", styles["GuideHeading"]),
        Paragraph("This guide explains how the Angular web application, ASP.NET Core API, SQL Server database and Python LSTM forecasting service work together. It describes the implementation currently present in this repository; it does not expose connection strings, server addresses, user names, passwords or other private infrastructure details.", styles["GuideBody"]),
        Paragraph("The project predicts seven days of rainfall, temperature and humidity. A separate explainable recommendation workflow uses the latest eligible stored forecast and clearly reports factors that are unavailable or only partially assessed.", styles["Callout"]),
        Paragraph("1. Overall architecture", styles["GuideHeading"]),
        ArchitectureDiagram(),
        Paragraph("The browser never communicates directly with SQL Server, Open-Meteo, or the Python model. Angular calls the ASP.NET Core API. The API owns authentication, data validation, SQL persistence and calls to the Python forecasting boundary.", styles["GuideBody"]),
        PageBreak(),
        Paragraph("2. The trained LSTM v1 model and Python FastAPI", styles["GuideHeading"]),
        Paragraph("The forecasting service is a Python FastAPI application. Its <b>WeatherForecastPredictor</b> loads the saved LSTM v1 model and its matching scaler. The API accepts exactly 30 consecutive daily observations containing rainfall, temperature and humidity, then returns seven dated predictions with the model version.", styles["GuideBody"]),
        table([
            ["Part", "Actual repository location", "Responsibility"],
            ["FastAPI endpoints", "src/forecasting-service/forecasting_service/api.py", "GET /health and POST /forecast"],
            ["Inference predictor", "forecasting_service/inference/predictor.py", "Validates 30 observations and produces seven predictions"],
            ["Saved artifacts", "src/forecasting-service/artifacts/v1_era5_2010_2025/", "v1.keras, scaler and evaluation metadata"],
            ["Evaluation", "forecasting_service/evaluation/metrics.py", "MAE and RMSE calculations"],
        ], [3.0 * cm, 7.0 * cm, 6.1 * cm], styles),
        Spacer(1, 0.3 * cm),
        Paragraph("The LSTM is a weather model, not a crop-selection model. Crop suitability is evaluated later by ASP.NET Core services using the persisted forecast and configuration/evidence that is available.", styles["Callout"]),
        Paragraph("3. ASP.NET Core to Python communication", styles["GuideHeading"]),
        *paragraph_list([
            "DependencyInjection.cs registers an HTTP client for IWeatherForecastingClient.",
            "PythonForecastingClient.cs sends the validated observation payload to Python POST /forecast.",
            "ProviderForecastOrchestrationService.cs checks the observation window, calls Python, validates seven output days and coordinates persistence.",
            "The backend response retains model version and operational provenance rather than treating operational input as historical ERA5 evaluation data.",
        ], styles),
        PageBreak(),
        Paragraph("4. Fresh operational forecast flow", styles["GuideHeading"]),
        SequenceDiagram(),
        Paragraph("The Open-Meteo adapter used for the controlled operational path is <b>OpenMeteoForecastPastDaysWeatherSource</b>. It requests hourly observations, aggregates them into local daily values, and fails closed when the required 30 complete days are not available. The administrator-only trigger is <b>POST /api/admin/forecasts/operational</b>.", styles["GuideBody"]),
        Paragraph("A successful run stores one parent ForecastRun marked Operational and seven child ForecastRecord rows. GET /api/forecasts/latest selects only a complete eligible operational run; it does not mix rows from different runs.", styles["GuideBody"]),
        Paragraph("5. SQL Server persistence and retrieval", styles["GuideHeading"]),
        table([
            ["Database concern", "Actual implementation"],
            ["EF Core context", "AnuradhapuraAiDbContext.cs in the Infrastructure project"],
            ["Forecast persistence", "ProviderForecastOrchestrationService.cs writes ForecastRun and ForecastRecord entities"],
            ["Read path", "ForecastService.cs and GET /api/forecasts/latest"],
            ["Recommendation read path", "RecommendationOrchestrationService.cs consumes the selected operational forecast"],
            ["Connection safety", "Development metadata is configuration; the password is held only in local user secrets"],
        ], [5.0 * cm, 11.1 * cm], styles),
        Paragraph("Connection concept: development configuration identifies a generic <b>&lt;server&gt;</b>, <b>&lt;database&gt;</b> and <b>&lt;user&gt;</b>; a local secret store supplies the password at runtime. The real values and the full connection string are intentionally excluded from this guide and from source control.", styles["Callout"]),
        PageBreak(),
        Paragraph("6. Angular to ASP.NET Core communication", styles["GuideHeading"]),
        Paragraph("Angular uses ApiService in <b>src/frontend/src/app/core/api.service.ts</b>. The service calls backend API routes through the local proxy during development. Components display backend results rather than connecting to SQL Server or Python directly.", styles["GuideBody"]),
        table([
            ["User interface", "Angular call", "Backend route", "What the user sees"],
            ["Public dashboard", "getLatestForecast()", "GET /api/forecasts/latest", "Latest persisted seven-day weather forecast and provenance"],
            ["Recommendations", "evaluateRecommendation()", "POST /api/recommendations/evaluate", "Forecast evidence, factor explanations and limitations"],
            ["Administrator", "generateOperationalForecast()", "POST /api/admin/forecasts/operational", "A new operational run summary"],
            ["Authentication", "login/register methods", "POST /api/auth/login and /api/auth/register", "Role-aware authenticated experience"],
        ], [3.0 * cm, 3.3 * cm, 4.5 * cm, 5.3 * cm], styles),
        Spacer(1, 0.25 * cm),
        Paragraph("The public dashboard component is public-dashboard.component.ts. It asks the API for the latest stored run and displays it only when it has seven daily values. recommendation-page.component.ts requests a read-only evaluation and presents missing factors explicitly, so a partial indicator is not presented as full scientific suitability.", styles["GuideBody"]),
        Paragraph("7. End-to-end example", styles["GuideHeading"]),
        table([
            ["Step", "Request or action", "Response / result"],
            ["1", "Administrator selects Generate operational forecast", "Angular POSTs /api/admin/forecasts/operational with the administrator token."],
            ["2", "Backend gets observations", "Open-Meteo adapter returns 30 complete daily values after validation and aggregation."],
            ["3", "Backend calls model", "Python POST /forecast returns seven v1 predictions."],
            ["4", "Backend saves run", "SQL stores one Operational ForecastRun and seven ForecastRecord rows atomically."],
            ["5", "Public dashboard loads", "GET /api/forecasts/latest returns the stored run, model version and seven days."],
            ["6", "User requests recommendation", "POST /api/recommendations/evaluate uses that same eligible run and returns evidence-aware crop results."],
        ], [1.0 * cm, 6.1 * cm, 9.0 * cm], styles),
        PageBreak(),
        Paragraph("8. Source-file map and endpoints", styles["GuideHeading"]),
        table([
            ["Area", "Key source files", "Relevant endpoints"],
            ["API setup", "Api/Program.cs; Infrastructure/DependencyInjection.cs", "/health"],
            ["Forecast API", "Api/Controllers/ForecastsController.cs; Infrastructure/Forecasting/ForecastService.cs", "GET /api/forecasts/latest; POST /api/forecasts"],
            ["Operational integration", "AdminController.cs; ProviderForecastOrchestrationService.cs; OpenMeteoForecastPastDaysWeatherSource.cs; PythonForecastingClient.cs", "POST /api/admin/forecasts/operational"],
            ["Recommendation", "RecommendationsController.cs; RecommendationOrchestrationService.cs", "POST /api/recommendations/evaluate"],
            ["Python model", "forecasting_service/api.py; inference/predictor.py", "GET /health; POST /forecast"],
            ["Frontend", "core/api.service.ts; public-dashboard.component.ts; recommendation-page.component.ts; admin-readiness.component.ts", "Calls the API routes above"],
        ], [3.0 * cm, 8.3 * cm, 4.8 * cm], styles),
        Paragraph("9. Run and verify the working application", styles["GuideHeading"]),
        *paragraph_list([
            "Keep the approved local development configuration and local user secret available. Do not place database passwords in appsettings files or source code.",
            "Start the Python service from src/forecasting-service: python -m uvicorn forecasting_service.api:app --host 127.0.0.1 --port 8000.",
            "Start the ASP.NET Core API from src/backend/AnuradhapuraAI.Api: dotnet run --urls http://127.0.0.1:5055.",
            "Start Angular from src/frontend: npm start -- --host 127.0.0.1 --port 4300.",
            "Verify /health on the API and Python service. Open the dashboard in the browser and confirm the latest forecast contains seven days.",
            "With an authorized Administrator account, use the Administration page to generate an operational forecast. Confirm the dashboard and recommendation page reference the same ForecastRun ID.",
        ], styles),
        Paragraph("Important limits: the operational integration is technically working, but historical ERA5 evaluation metrics do not establish live Open-Meteo operational accuracy. Crop results remain evidence-aware and should not be presented as guaranteed agronomic outcomes.", styles["Callout"]),
    ])
    document.build(story, onFirstPage=page_number, onLaterPages=page_number)


if __name__ == "__main__":
    build_document()
    print(OUTPUT)
