using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

const int recordIndex = 0;
const double positionSpikeThreshold = 0.01;
const double rotationSpikeThresholdDegrees = 5.0;

string inputDir = args.Length > 0 ? args[0] : Path.Combine("Assets", "RecordData");
string outputPath = args.Length > 1
    ? args[1]
    : Path.Combine(inputDir, "RecordData_Quality_Box0_Box1_Main.html");

if (!Directory.Exists(inputDir))
{
    Console.Error.WriteLine($"Input directory not found: {inputDir}");
    return 1;
}

Console.WriteLine($"Reading {inputDir}");
Series[] charts =
[
    new("Box0 Pos Y", "m", "#246bfe", ReadBoxNumber(inputDir, "BoxPosData.json", 0, "y"), positionSpikeThreshold, SpikeMode.SingleFrame),
    new("Box1 Pos Y", "m", "#d13f31", ReadBoxNumber(inputDir, "BoxPosData.json", 1, "y"), positionSpikeThreshold, SpikeMode.SingleFrame),
    new("Box0 Rot Delta", "deg/frame", "#247a3d", ReadBoxRotationDelta(inputDir, 0), rotationSpikeThresholdDegrees, SpikeMode.AboveThreshold),
    new("Box1 Rot Delta", "deg/frame", "#a15c00", ReadBoxRotationDelta(inputDir, 1), rotationSpikeThresholdDegrees, SpikeMode.AboveThreshold),
    new("Main Pos X", "m", "#246bfe", ReadMainNumber(inputDir, "MainPosData.json", "x"), positionSpikeThreshold, SpikeMode.SingleFrame),
    new("Main Pos Y", "m", "#d13f31", ReadMainNumber(inputDir, "MainPosData.json", "y"), positionSpikeThreshold, SpikeMode.SingleFrame),
    new("Main Pos Z", "m", "#247a3d", ReadMainNumber(inputDir, "MainPosData.json", "z"), positionSpikeThreshold, SpikeMode.SingleFrame),
    new("Main Rot Delta", "deg/frame", "#7c3aed", ReadMainRotationDelta(inputDir), rotationSpikeThresholdDegrees, SpikeMode.AboveThreshold),
];

foreach (Series series in charts)
{
    series.FindSpikes();
    Console.WriteLine($"{series.Name}: range={series.Range:F6}{series.Unit} maxStep={series.MaxStep:F6}{series.Unit} spikes={series.Spikes.Count}");
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
File.WriteAllText(outputPath, BuildHtml(inputDir, charts), new UTF8Encoding(false));
Console.WriteLine($"Wrote {outputPath}");
return 0;

static double[] ReadBoxNumber(string dir, string fileName, int boxIndex, string propertyName)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, fileName)));
    JsonElement record = doc.RootElement[recordIndex];
    double[] values = new double[record.GetArrayLength()];
    int i = 0;
    foreach (JsonElement frame in record.EnumerateArray())
    {
        values[i++] = frame[boxIndex].GetProperty(propertyName).GetDouble();
    }

    return values;
}

static double[] ReadMainNumber(string dir, string fileName, string propertyName)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, fileName)));
    JsonElement record = doc.RootElement[recordIndex];
    double[] values = new double[record.GetArrayLength()];
    int i = 0;
    foreach (JsonElement frame in record.EnumerateArray())
    {
        values[i++] = frame.GetProperty(propertyName).GetDouble();
    }

    return values;
}

static double[] ReadBoxRotationDelta(string dir, int boxIndex)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, "BoxRotData.json")));
    JsonElement record = doc.RootElement[recordIndex];
    double[] values = new double[record.GetArrayLength()];
    Quaternion previous = default;
    int i = 0;
    foreach (JsonElement frame in record.EnumerateArray())
    {
        Quaternion current = ReadQuaternion(frame[boxIndex]);
        values[i] = i == 0 ? 0 : QuaternionAngle(previous, current);
        previous = current;
        i++;
    }

    return values;
}

static double[] ReadMainRotationDelta(string dir)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, "MainRotData.json")));
    JsonElement record = doc.RootElement[recordIndex];
    double[] values = new double[record.GetArrayLength()];
    Quaternion previous = default;
    int i = 0;
    foreach (JsonElement frame in record.EnumerateArray())
    {
        Quaternion current = ReadQuaternion(frame);
        values[i] = i == 0 ? 0 : QuaternionAngle(previous, current);
        previous = current;
        i++;
    }

    return values;
}

static Quaternion ReadQuaternion(JsonElement element)
{
    return new Quaternion(
        element.GetProperty("x").GetDouble(),
        element.GetProperty("y").GetDouble(),
        element.GetProperty("z").GetDouble(),
        element.GetProperty("w").GetDouble());
}

static double QuaternionAngle(Quaternion a, Quaternion b)
{
    double dot = Math.Abs(a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W);
    dot = Math.Clamp(dot, -1.0, 1.0);
    return 2.0 * Math.Acos(dot) * 180.0 / Math.PI;
}

static string BuildHtml(string inputDir, IReadOnlyList<Series> series)
{
    string title = JavaScriptEncoder.Default.Encode(inputDir.Replace('\\', '/'));
    string data = string.Join(",\n", series.Select((s, i) => $$"""
      {
        group: {{GroupIndex(i)}},
        name: "{{JavaScriptEncoder.Default.Encode(s.Name)}}",
        unit: "{{s.Unit}}",
        color: "{{s.Color}}",
        values: [{{JoinNumbers(s.Values)}}],
        spikes: [{{string.Join(",", s.Spikes)}}],
        range: "{{Fmt(s.Range)}}",
        maxStep: "{{Fmt(s.MaxStep)}}"
      }
"""));

    return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Record Data Quality</title>
  <style>
    body { margin: 0; padding: 20px; font-family: Arial, Helvetica, sans-serif; background: #f6f7f9; color: #17202a; }
    h1 { margin: 0 0 6px; font-size: 22px; }
    .meta { color: #5f6b78; font-size: 13px; margin-bottom: 16px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
    .card { background: white; border: 1px solid #d9dee5; border-radius: 8px; padding: 12px; box-shadow: 0 1px 3px rgba(15, 23, 42, .06); }
    h2 { margin: 0 0 8px; font-size: 15px; }
    canvas { width: 100%; height: 320px; display: block; }
    table { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: 12px; }
    th, td { border-bottom: 1px solid #e7ebf0; padding: 5px; text-align: right; }
    th:first-child, td:first-child { text-align: left; }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <h1>Record Data Quality</h1>
  <div class="meta">Source: {{title}} · Record 0 · Red dots mark detected spikes/outliers</div>
  <div class="grid">
    <section class="card"><h2>Box Position Y</h2><canvas id="chart0"></canvas><table id="table0"></table></section>
    <section class="card"><h2>Box Rotation Delta</h2><canvas id="chart1"></canvas><table id="table1"></table></section>
    <section class="card"><h2>Main Position XYZ</h2><canvas id="chart2"></canvas><table id="table2"></table></section>
    <section class="card"><h2>Main Rotation Delta</h2><canvas id="chart3"></canvas><table id="table3"></table></section>
  </div>
  <script>
    const series = [
{{data}}
    ];

    window.addEventListener("resize", drawAll);
    drawAll();

    function drawAll() {
      for (let group = 0; group < 4; group++) {
        const items = series.filter(s => s.group === group);
        drawChart(document.getElementById("chart" + group), items);
        drawTable(document.getElementById("table" + group), items);
      }
    }

    function drawTable(table, items) {
      table.innerHTML = "<thead><tr><th>Series</th><th>Range</th><th>Max step</th><th>Spikes</th></tr></thead>" +
        "<tbody>" + items.map(s => `<tr><td style="color:${s.color}">${s.name}</td><td>${s.range}</td><td>${s.maxStep}</td><td>${s.spikes.length}</td></tr>`).join("") + "</tbody>";
    }

    function drawChart(canvas, items) {
      const ctx = canvas.getContext("2d");
      const rect = canvas.getBoundingClientRect();
      const dpr = window.devicePixelRatio || 1;
      canvas.width = Math.max(1, Math.round(rect.width * dpr));
      canvas.height = Math.max(1, Math.round(rect.height * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

      const w = rect.width, h = rect.height;
      const pad = { left: 52, right: 12, top: 14, bottom: 34 };
      ctx.clearRect(0, 0, w, h);
      ctx.fillStyle = "#fff"; ctx.fillRect(0, 0, w, h);

      let min = Infinity, max = -Infinity, frames = 0;
      for (const s of items) {
        frames = Math.max(frames, s.values.length);
        for (const v of s.values) { if (v < min) min = v; if (v > max) max = v; }
      }
      if (!isFinite(min) || min === max) { min -= 1; max += 1; }
      const margin = Math.max((max - min) * 0.08, 0.0001);
      min -= margin; max += margin;

      const pw = w - pad.left - pad.right, ph = h - pad.top - pad.bottom;
      const xOf = i => pad.left + i / Math.max(1, frames - 1) * pw;
      const yOf = v => pad.top + (max - v) / (max - min) * ph;

      ctx.strokeStyle = "#e3e8ef"; ctx.lineWidth = 1; ctx.font = "11px Arial"; ctx.fillStyle = "#66717d";
      for (let i = 0; i <= 4; i++) {
        const v = min + (max - min) * i / 4, y = yOf(v);
        ctx.beginPath(); ctx.moveTo(pad.left, y); ctx.lineTo(w - pad.right, y); ctx.stroke();
        ctx.textAlign = "right"; ctx.textBaseline = "middle"; ctx.fillText(v.toFixed(3), pad.left - 6, y);
      }
      ctx.strokeStyle = "#9aa5b1"; ctx.strokeRect(pad.left, pad.top, pw, ph);

      for (const s of items) {
        const step = Math.max(1, Math.floor(s.values.length / Math.max(500, pw)));
        ctx.strokeStyle = s.color; ctx.lineWidth = 1.5; ctx.beginPath();
        for (let i = 0; i < s.values.length; i += step) {
          const x = xOf(i), y = yOf(s.values[i]);
          if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
        ctx.fillStyle = "#e02f2f";
        for (const frame of s.spikes) {
          ctx.beginPath(); ctx.arc(xOf(frame), yOf(s.values[frame]), 3, 0, Math.PI * 2); ctx.fill();
        }
      }
    }
  </script>
</body>
</html>
""";
}

static int GroupIndex(int seriesIndex) => seriesIndex switch
{
    0 or 1 => 0,
    2 or 3 => 1,
    4 or 5 or 6 => 2,
    _ => 3
};

static string JoinNumbers(IReadOnlyList<double> values) => string.Join(",", values.Select(Fmt));
static string Fmt(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);

enum SpikeMode { SingleFrame, AboveThreshold }

record struct Quaternion(double X, double Y, double Z, double W);

sealed class Series
{
    public Series(string name, string unit, string color, double[] values, double threshold, SpikeMode spikeMode)
    {
        Name = name;
        Unit = unit;
        Color = color;
        Values = values;
        Threshold = threshold;
        SpikeMode = spikeMode;
        Range = values.Max() - values.Min();
        MaxStep = values.Zip(values.Skip(1), (a, b) => Math.Abs(b - a)).DefaultIfEmpty(0).Max();
    }

    public string Name { get; }
    public string Unit { get; }
    public string Color { get; }
    public double[] Values { get; }
    public double Threshold { get; }
    public SpikeMode SpikeMode { get; }
    public double Range { get; }
    public double MaxStep { get; }
    public List<int> Spikes { get; } = new();

    public void FindSpikes()
    {
        if (SpikeMode == SpikeMode.AboveThreshold)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                if (Values[i] >= Threshold)
                {
                    Spikes.Add(i);
                }
            }

            return;
        }

        for (int i = 1; i < Values.Length - 1; i++)
        {
            double neighborDelta = Math.Abs(Values[i + 1] - Values[i - 1]);
            double spike = Math.Abs(Values[i] - ((Values[i - 1] + Values[i + 1]) * 0.5));
            if (spike >= Threshold && neighborDelta <= Threshold)
            {
                Spikes.Add(i);
            }
        }
    }
}
