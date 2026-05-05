using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

const int recordIndex = 0;
const int firstBox = 0;
const int secondBox = 1;
const double spikeThresholdMeters = 0.01;

string inputPath = args.Length > 0 ? args[0] : Path.Combine("Assets", "RecordData", "BoxPosData.json");
string outputPath = args.Length > 1 ? args[1] : Path.Combine("Assets", "RecordData", "BoxPosData_Box0_Box1.html");

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

Console.WriteLine($"Reading {inputPath}");
using FileStream stream = File.OpenRead(inputPath);
using JsonDocument document = JsonDocument.Parse(stream);

JsonElement root = document.RootElement;
if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() <= recordIndex)
{
    Console.Error.WriteLine("Unexpected BoxPosData shape: expected records array.");
    return 1;
}

JsonElement record = root[recordIndex];
int frameCount = record.GetArrayLength();
double[] box0Y = new double[frameCount];
double[] box1Y = new double[frameCount];

int frameIndex = 0;
foreach (JsonElement frame in record.EnumerateArray())
{
    box0Y[frameIndex] = frame[firstBox].GetProperty("y").GetDouble();
    box1Y[frameIndex] = frame[secondBox].GetProperty("y").GetDouble();
    frameIndex++;
}

SeriesSummary box0Summary = Summarize(firstBox, box0Y);
SeriesSummary box1Summary = Summarize(secondBox, box1Y);

string html = BuildHtml(inputPath, frameCount, box0Summary, box1Summary);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine($"Wrote {outputPath}");
Console.WriteLine($"{box0Summary.Name}: yRange={box0Summary.YRange:F6}m maxAbsDy={box0Summary.MaxAbsDy:F6}m spikes1cm={box0Summary.Spikes.Count}");
Console.WriteLine($"{box1Summary.Name}: yRange={box1Summary.YRange:F6}m maxAbsDy={box1Summary.MaxAbsDy:F6}m spikes1cm={box1Summary.Spikes.Count}");
return 0;

static SeriesSummary Summarize(int boxIndex, double[] yValues)
{
    double min = yValues.Min();
    double max = yValues.Max();
    double maxAbsDy = 0.0;
    double sumAbsDy = 0.0;

    for (int i = 0; i < yValues.Length - 1; i++)
    {
        double absDy = Math.Abs(yValues[i + 1] - yValues[i]);
        maxAbsDy = Math.Max(maxAbsDy, absDy);
        sumAbsDy += absDy;
    }

    List<Spike> spikes = new();
    for (int i = 1; i < yValues.Length - 1; i++)
    {
        double neighborDelta = Math.Abs(yValues[i + 1] - yValues[i - 1]);
        double spike = Math.Abs(yValues[i] - ((yValues[i - 1] + yValues[i + 1]) * 0.5));
        if (spike >= spikeThresholdMeters && neighborDelta <= spikeThresholdMeters)
        {
            spikes.Add(new Spike(i, spike, yValues[i - 1], yValues[i], yValues[i + 1]));
        }
    }

    return new SeriesSummary(
        $"Box {boxIndex}",
        yValues,
        min,
        max,
        max - min,
        maxAbsDy,
        sumAbsDy / Math.Max(1, yValues.Length - 1),
        spikes);
}

static string BuildHtml(string inputPath, int frameCount, SeriesSummary box0, SeriesSummary box1)
{
    string sourceName = JavaScriptEncoder.Default.Encode(inputPath.Replace('\\', '/'));
    return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Box Position Y Visualization</title>
  <style>
    :root {
      color-scheme: light;
      font-family: Arial, Helvetica, sans-serif;
      background: #f6f7f9;
      color: #17202a;
    }
    body {
      margin: 0;
      padding: 24px;
    }
    h1 {
      font-size: 22px;
      margin: 0 0 6px;
    }
    .meta {
      color: #55606b;
      font-size: 13px;
      margin-bottom: 18px;
    }
    .layout {
      display: grid;
      grid-template-columns: 260px minmax(0, 1fr);
      gap: 18px;
      align-items: start;
    }
    .panel, .chart-wrap {
      background: #ffffff;
      border: 1px solid #d9dee5;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);
    }
    .panel {
      padding: 14px;
    }
    .chart-wrap {
      padding: 14px;
    }
    canvas {
      display: block;
      width: 100%;
      height: 560px;
    }
    label {
      display: flex;
      gap: 8px;
      align-items: center;
      margin: 8px 0;
      font-size: 14px;
    }
    .swatch {
      width: 12px;
      height: 12px;
      border-radius: 2px;
      display: inline-block;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 14px;
      font-size: 12px;
    }
    th, td {
      text-align: right;
      border-bottom: 1px solid #e6eaf0;
      padding: 6px 4px;
    }
    th:first-child, td:first-child {
      text-align: left;
    }
    .hint {
      color: #66717d;
      font-size: 12px;
      line-height: 1.45;
      margin-top: 12px;
    }
    @media (max-width: 860px) {
      body { padding: 14px; }
      .layout { grid-template-columns: 1fr; }
      canvas { height: 420px; }
    }
  </style>
</head>
<body>
  <h1>Box Position Y Visualization</h1>
  <div class="meta">Source: {{sourceName}} · Record 0 · Frames: {{frameCount}} · Spike threshold: {{Format(spikeThresholdMeters)}} m</div>
  <div class="layout">
    <aside class="panel">
      <label><input id="box0Toggle" type="checkbox" checked><span class="swatch" style="background:#246bfe"></span>{{box0.Name}}</label>
      <label><input id="box1Toggle" type="checkbox" checked><span class="swatch" style="background:#d13f31"></span>{{box1.Name}}</label>
      <table>
        <thead>
          <tr><th>Series</th><th>Y range</th><th>Max dy</th><th>Spikes</th></tr>
        </thead>
        <tbody>
          <tr><td>{{box0.Name}}</td><td>{{Format(box0.YRange)}}</td><td>{{Format(box0.MaxAbsDy)}}</td><td>{{box0.Spikes.Count}}</td></tr>
          <tr><td>{{box1.Name}}</td><td>{{Format(box1.YRange)}}</td><td>{{Format(box1.MaxAbsDy)}}</td><td>{{box1.Spikes.Count}}</td></tr>
        </tbody>
      </table>
      <div class="hint">
        Red circular markers indicate one-frame Y spikes: the middle frame deviates by at least 1 cm while neighboring frames stay close.
      </div>
    </aside>
    <main class="chart-wrap">
      <canvas id="chart"></canvas>
    </main>
  </div>
  <script>
    const frameCount = {{frameCount}};
    const series = [
      {
        name: "{{box0.Name}}",
        color: "#246bfe",
        enabledId: "box0Toggle",
        y: [{{JoinNumbers(box0.YValues)}}],
        spikes: [{{JoinSpikeFrames(box0.Spikes)}}]
      },
      {
        name: "{{box1.Name}}",
        color: "#d13f31",
        enabledId: "box1Toggle",
        y: [{{JoinNumbers(box1.YValues)}}],
        spikes: [{{JoinSpikeFrames(box1.Spikes)}}]
      }
    ];

    const canvas = document.getElementById("chart");
    const ctx = canvas.getContext("2d");
    const toggles = series.map(s => document.getElementById(s.enabledId));
    toggles.forEach(t => t.addEventListener("change", draw));
    window.addEventListener("resize", draw);

    function resizeCanvas() {
      const dpr = window.devicePixelRatio || 1;
      const rect = canvas.getBoundingClientRect();
      canvas.width = Math.max(1, Math.round(rect.width * dpr));
      canvas.height = Math.max(1, Math.round(rect.height * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      return rect;
    }

    function draw() {
      const rect = resizeCanvas();
      const w = rect.width;
      const h = rect.height;
      const pad = { left: 58, right: 18, top: 18, bottom: 42 };
      const visible = series.filter(s => document.getElementById(s.enabledId).checked);
      ctx.clearRect(0, 0, w, h);
      ctx.fillStyle = "#ffffff";
      ctx.fillRect(0, 0, w, h);
      if (visible.length === 0) return;

      let minY = Infinity;
      let maxY = -Infinity;
      for (const s of visible) {
        for (const y of s.y) {
          if (y < minY) minY = y;
          if (y > maxY) maxY = y;
        }
      }
      const margin = Math.max(0.02, (maxY - minY) * 0.08);
      minY -= margin;
      maxY += margin;

      const plotW = w - pad.left - pad.right;
      const plotH = h - pad.top - pad.bottom;
      const xOf = frame => pad.left + (frame / Math.max(1, frameCount - 1)) * plotW;
      const yOf = y => pad.top + (maxY - y) / Math.max(0.000001, maxY - minY) * plotH;

      drawGrid(pad, w, h, minY, maxY, xOf, yOf);
      for (const s of visible) drawSeries(s, xOf, yOf, plotW);
      drawLegend(visible, w, pad);
    }

    function drawGrid(pad, w, h, minY, maxY, xOf, yOf) {
      ctx.strokeStyle = "#e2e7ee";
      ctx.lineWidth = 1;
      ctx.fillStyle = "#5d6874";
      ctx.font = "12px Arial";
      ctx.textBaseline = "middle";
      ctx.textAlign = "right";
      for (let i = 0; i <= 5; i++) {
        const yValue = minY + (maxY - minY) * (i / 5);
        const y = yOf(yValue);
        ctx.beginPath();
        ctx.moveTo(pad.left, y);
        ctx.lineTo(w - pad.right, y);
        ctx.stroke();
        ctx.fillText(yValue.toFixed(3), pad.left - 8, y);
      }
      ctx.textBaseline = "top";
      ctx.textAlign = "center";
      for (let i = 0; i <= 5; i++) {
        const frame = Math.round((frameCount - 1) * (i / 5));
        const x = xOf(frame);
        ctx.beginPath();
        ctx.moveTo(x, pad.top);
        ctx.lineTo(x, h - pad.bottom);
        ctx.stroke();
        ctx.fillText(frame.toString(), x, h - pad.bottom + 10);
      }
      ctx.strokeStyle = "#9aa5b1";
      ctx.strokeRect(pad.left, pad.top, w - pad.left - pad.right, h - pad.top - pad.bottom);
    }

    function drawSeries(s, xOf, yOf, plotW) {
      const step = Math.max(1, Math.floor(s.y.length / Math.max(700, plotW)));
      ctx.strokeStyle = s.color;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      for (let i = 0; i < s.y.length; i += step) {
        const x = xOf(i);
        const y = yOf(s.y[i]);
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      ctx.fillStyle = "#e02f2f";
      for (const frame of s.spikes) {
        const x = xOf(frame);
        const y = yOf(s.y[frame]);
        ctx.beginPath();
        ctx.arc(x, y, 3, 0, Math.PI * 2);
        ctx.fill();
      }
    }

    function drawLegend(visible, w, pad) {
      ctx.textAlign = "left";
      ctx.textBaseline = "top";
      ctx.font = "12px Arial";
      let x = pad.left + 8;
      const y = pad.top + 8;
      for (const s of visible) {
        ctx.fillStyle = s.color;
        ctx.fillRect(x, y + 2, 12, 12);
        ctx.fillStyle = "#24313f";
        ctx.fillText(`${s.name} (${s.spikes.length} spikes)`, x + 18, y);
        x += 150;
      }
    }

    draw();
  </script>
</body>
</html>
""";
}

static string JoinNumbers(IReadOnlyList<double> values)
{
    StringBuilder builder = new(values.Count * 10);
    for (int i = 0; i < values.Count; i++)
    {
        if (i > 0)
        {
            builder.Append(',');
        }

        builder.Append(Format(values[i]));
    }

    return builder.ToString();
}

static string JoinSpikeFrames(IReadOnlyList<Spike> spikes)
{
    return string.Join(",", spikes.Select(spike => spike.Frame.ToString(CultureInfo.InvariantCulture)));
}

static string Format(double value)
{
    return value.ToString("0.########", CultureInfo.InvariantCulture);
}

record Spike(int Frame, double Amount, double PreviousY, double CurrentY, double NextY);

record SeriesSummary(
    string Name,
    double[] YValues,
    double MinY,
    double MaxY,
    double YRange,
    double MaxAbsDy,
    double MeanAbsDy,
    List<Spike> Spikes);
