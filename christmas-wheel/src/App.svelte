<script lang="ts">
  type Segment = {
    label: string;
    weight: number;
    color: string;
  };

  type Slice = Segment & {
    startAngle: number;
    endAngle: number;
    midAngle: number;
  };

  const palette = ["#f4b400", "#4285f4", "#0f9d58"];

  let baseSegments = [
    { label: "75%", weight: 47.5 },
    { label: "100%", weight: 5 },
    { label: "50%", weight: 47.5 },
  ];

  let segments: Segment[] = baseSegments.map((s, i) => ({
    ...s,
    color: palette[i % palette.length],
  }));

  let rotation = 0;
  let spinning = false;
  let hasSpun = false;
  let winningSlice: Slice | null = null;

  // On load, check if there's already a spin result stored on the server
  if (typeof window !== 'undefined') {
    fetch(`https://julehjulapi.madpro.dk/spinresult`)
      .then((res) => {
        if (!res.ok) return;
        return res.json();
      })
      .then((data) => {
        if (!data || !data.label) return;
        hasSpun = true;
        // Find matching slice by label (labels are like "75%", "50%", "100%")
        winningSlice = slices.find((s) => s.label === data.label) ?? null;
      })
      .catch(() => {});
  }

  $: slices = (() => {
    const total = segments.reduce((s, seg) => s + seg.weight, 0);
    let acc = 0;

    return segments.map((seg) => {
      const angle = (seg.weight / total) * 360;
      const start = acc;
      const end = acc + angle;
      const slice: Slice = {
        ...seg,
        startAngle: start,
        endAngle: end,
        midAngle: (start + end) / 2,
      };
      acc += angle;
      return slice;
    });
  })();

  function px(a: number, r: number) {
    return 50 + r * Math.sin((Math.PI * a) / 180);
  }

  function py(a: number, r: number) {
    return 50 - r * Math.cos((Math.PI * a) / 180);
  }

  function slicePath(s: Slice, r = 50) {
    const large = s.endAngle - s.startAngle > 180 ? 1 : 0;
    return `
      M50,50
      L${px(s.startAngle, r)},${py(s.startAngle, r)}
      A${r},${r} 0 ${large} 1 ${px(s.endAngle, r)},${py(s.endAngle, r)}
      Z
    `;
  }

  function spin() {
    if (spinning || hasSpun) return;
    spinning = true;
    winningSlice = null;

    const r = Math.random() * 360;
    const target = slices.find((s) => r >= s.startAngle && r < s.endAngle)!;
    const mid = (target.startAngle + target.endAngle) / 2;

    rotation += 360 * 6 + (360 - mid);

    setTimeout(() => {
      spinning = false;
      winningSlice = target;
      sendMail(target.label);
      hasSpun = true;
    }, 3000);
  }

  function sendMail(label: string) {
    // Map the segment label to the API's Roll enum name (API expects string enum values)
    let rollName = "FullPrice";

    if (label.includes("75")) {
      rollName = "SeventyFivePercent";
    } else if (label.includes("50")) {
      rollName = "HalfPrice";
    } else if (label.includes("100")) {
      rollName = "FullPrice";
    }

    // POST to the API service by its docker-compose service name `api` on the internal port 5000
    // The API uses a JsonStringEnumConverter so we send the enum as a string.
    fetch("https://julehjulapi.madpro.dk/sendresult", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Roll: rollName }),
    })
      .then((res) => {
        if (!res.ok) throw new Error(`sendresult failed: ${res.status}`);
      })
      .catch((err) => {
        console.error("sendMail error:", err);
      });
  }
</script>

<div class="container">
  <h1 class="title">🎄 Christmas Wheel 🎄</h1>
  <h2>Rul og vind et par valgfri klatresko! 👟</h2>
  <div class="container">
    <div class="wheel-wrapper">
      <div class="pointer"></div>

      <svg
        viewBox="0 0 100 100"
        class="wheel"
        style="transform: rotate({rotation}deg)"
      >
        <circle cx="50" cy="50" r="50" fill="Canvas" />

        {#each slices as slice}
          <path
            d={slicePath(slice)}
            fill={slice.color}
            class:win={winningSlice === slice}
          />

          <text
            x={px(slice.midAngle, 28)}
            y={py(slice.midAngle, 28)}
            text-anchor="middle"
            dominant-baseline="middle"
            fill="CanvasText"
            font-size="7"
            transform={`rotate(
    ${slice.midAngle > 180 ? slice.midAngle - 90 : slice.midAngle + 90}
    ${px(slice.midAngle, 28)}
    ${py(slice.midAngle, 28)}
  )`}
          >
            {slice.label}
          </text>
        {/each}
      </svg>
    </div>

    <button on:click={spin} disabled={spinning || hasSpun}>
      {spinning ? "Spinning…" : hasSpun ? "Already spun" : "Spin"}
    </button>

    {#if hasSpun}
      <div style="margin-top:8px;color:gray;font-size:0.9rem">Du har allerede drejet hjulet og har vundet! {winningSlice?.label}</div>
    {/if}

    {#if winningSlice}
      <strong>🎉 Du har rullet og vundet {winningSlice.label} på et par klatresko!</strong>
    {/if}
  </div>
</div>

<style>
  .container {
    min-height: 100vh;
    background: Canvas;
    color: CanvasText;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
  }

  .wheel-wrapper {
    width: 320px;
    height: 320px;
    position: relative;
  }

  .wheel {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    border: 4px solid CanvasText;
    transition: transform 3s cubic-bezier(0.33, 1, 0.68, 1);
  }

  .pointer {
    position: absolute;
    top: -18px;
    left: 50%;
    transform: translateX(-50%);
    border-left: 14px solid transparent;
    border-right: 14px solid transparent;
    border-top: 28px solid red;
    z-index: 10;
  }

  .win {
    filter: drop-shadow(0 0 6px gold);
  }
</style>
