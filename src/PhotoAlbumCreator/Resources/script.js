
(function () {
  "use strict";

  const gallery = document.querySelector("#gallery");
  if (!gallery) return;

  /** @returns {HTMLElement[]} */
  const cardsAll = () => Array.from(gallery.querySelectorAll(".card[data-type]"));

  const lb = buildLightbox();
  let index = -1;

  // -------------------------------
  // Lightbox
  // -------------------------------
  function buildLightbox() {
    const root = document.getElementById("lightbox");
    const stage = root.querySelector(".lb-stage");
    const counter = root.querySelector(".lb-counter .count");
    const total = root.querySelector(".lb-counter .total");
    const prevBtn = root.querySelector(".lb-arrow.prev");
    const nextBtn = root.querySelector(".lb-arrow.next");
    const closeBtn = root.querySelector(".lb-btn.close");
    const fitBtn = root.querySelector(".lb-btn.fit");
    const fsBtn = root.querySelector(".lb-btn.fs");

    // Keyboard shortcuts
    window.addEventListener("keydown", (e) => {
      if (root.getAttribute("aria-hidden") === "true") return;

      if (e.key === "Escape") {
        close();
      } else if (e.key === "ArrowRight") {
        next();
      } else if (e.key === "ArrowLeft") {
        prev();
      } else if (e.key === "+") {
        zoomBy(0.2);
      } else if (e.key === "-") {
        zoomBy(-0.2);
      } else if (e.key.toLowerCase() === "f") {
        toggleFit();
      }
    });

    prevBtn.addEventListener("click", prev);
    nextBtn.addEventListener("click", next);
    closeBtn.addEventListener("click", close);
    fitBtn.addEventListener("click", toggleFit);
    fsBtn.addEventListener("click", toggleFullscreen);

    let scale = 1;
    let originX = 0;
    let originY = 0;
    let lastX = 0;
    let lastY = 0;
    let isPanning = false;

    let mediaNode = null;

    const MAX_SCALE = 6;
    const MIN_SCALE = 1;

    const clamp = (v, a, b) => Math.min(Math.max(v, a), b);

    function applyTransform() {
      if (mediaNode) {
        mediaNode.style.transform = `translate(${originX}px, ${originY}px) scale(${scale})`;
      }
    }

    function resetTransform() {
      scale = 1;
      originX = 0;
      originY = 0;
      applyTransform();
    }

    function zoom(delta, cx, cy) {
      const prev = scale;
      scale = clamp(scale + delta, MIN_SCALE, MAX_SCALE);

      if (scale !== prev) {
        originX = (originX - cx) * (scale / prev) + cx;
        originY = (originY - cy) * (scale / prev) + cy;
        applyTransform();
      }
    }

    function zoomBy(step) {
      const cx = (mediaNode?.clientWidth || 0) / 2;
      const cy = (mediaNode?.clientHeight || 0) / 2;
      zoom(step, cx, cy);
    }

    // Mouse wheel zoom (images only)
    stage.addEventListener(
      "wheel",
      (e) => {
        if (!mediaNode || mediaNode.tagName !== "IMG") return;
        e.preventDefault();

        const delta = -Math.sign(e.deltaY) * 0.15;
        const cx = mediaNode.clientWidth / 2;
        const cy = mediaNode.clientHeight / 2;
        zoom(delta, cx, cy);
      },
      { passive: false }
    );

    // Double click to toggle zoom (image)
    stage.addEventListener("dblclick", () => {
      if (!mediaNode || mediaNode.tagName !== "IMG") return;
      if (scale > 1) {
        resetTransform();
      } else {
        zoom(1, mediaNode.clientWidth / 2, mediaNode.clientHeight / 2);
      }
    });

    // Pan when zoomed in
    stage.addEventListener("pointerdown", (e) => {
      if (!mediaNode || mediaNode.tagName !== "IMG") return;
      if (scale <= 1) return;

      mediaNode.setPointerCapture(e.pointerId);
      isPanning = true;
      lastX = e.clientX;
      lastY = e.clientY;
      stage.style.cursor = "grabbing";
    });

    stage.addEventListener("pointermove", (e) => {
      if (!isPanning || mediaNode?.tagName !== "IMG") return;

      originX += e.clientX - lastX;
      originY += e.clientY - lastY;
      lastX = e.clientX;
      lastY = e.clientY;
      applyTransform();
    });

    const endPan = () => {
      isPanning = false;
      stage.style.cursor = "default";
    };

    stage.addEventListener("pointerup", endPan);
    stage.addEventListener("pointercancel", endPan);

    // Click outside media closes
    stage.addEventListener("click", (e) => {
      const clickedMedia = e.target.closest(".lb-media");
      const clickedControl = e.target.closest(".lb-arrow, .lb-btn");
      if (!clickedMedia && !clickedControl) close();
    });

    function render(i) {
      stage.innerHTML = "";
      mediaNode = null;
      resetTransform();

      const cards = cardsAll();
      const card = cards[i];
      if (!card) return;

      const type = card.dataset.type;
      const src = card.dataset.src;
      const alt =
        (card.querySelector("img")?.alt ||
          card.querySelector("figcaption")?.textContent ||
          "").trim();

      if (type === "video") {
        const vid = document.createElement("video");
        vid.src = src;
        vid.className = "lb-media";
        vid.controls = true;
        vid.playsInline = true;
        vid.preload = "metadata";
        vid.autoplay = true;
        stage.appendChild(vid);
        mediaNode = vid;
      } else {
        const img = document.createElement("img");
        img.src = src;
        img.alt = alt;
        img.className = "lb-media";
        img.draggable = false;
        img.decoding = "async";
        stage.appendChild(img);
        mediaNode = img;
      }

      counter.textContent = (i + 1).toString();
      total.textContent = cards.length.toString();
    }

    function open(i) {
      render(i);
      root.setAttribute("aria-hidden", "false");
      document.documentElement.style.overflow = "hidden";
    }

    function close() {
      root.setAttribute("aria-hidden", "true");
      document.documentElement.style.overflow = "";
      stage.innerHTML = "";
    }

    function next() {
      index = (index + 1) % cardsAll().length;
      render(index);
    }

    function prev() {
      index = (index - 1 + cardsAll().length) % cardsAll().length;
      render(index);
    }

    function toggleFit() {
      if (!stage.firstElementChild) return;
      const el = stage.firstElementChild;

      if (el.style.maxWidth) {
        el.style.maxWidth = "";
        el.style.maxHeight = "";
      } else {
        el.style.maxWidth = "100vw";
        el.style.maxHeight = "100vh";
      }
    }

    function toggleFullscreen() {
      const el = stage.firstElementChild;
      if (!el) return;

      if (document.fullscreenElement) {
        document.exitFullscreen();
      } else {
        el.requestFullscreen?.();
      }
    }

    return { open, close, next, prev, render };
  }

  function open(i) {
    index = i;
    lb.open(i);
  }

  // Open from gallery click
    gallery.addEventListener("click", (e) => {
        const link = e.target.closest(".media");
        if (!link) return;

        // Ищем карточку с data-type (image / video).
        // Если нет — даём ссылке работать как обычно (папки, внешние ссылки и т.п.)
        const card = link.closest(".card[data-type]");
        if (!card) {
            return;
        }

        e.preventDefault();
        const cards = cardsAll();
        const i = cards.indexOf(card);
        if (i >= 0) open(i);
    });

  // -------------------------------
  // Lazy load / unload
  // -------------------------------
  const PLACEHOLDER = "data:image/gif;base64,R0lGODlhAQABAAAAACw=";

  function primeCard(card) {
    if (card.dataset.primed) return;
    card.dataset.primed = "1";

    const type = card.dataset.type;
    const src = card.dataset.src;
    const media = card.querySelector("img, video");

    if (type === "image" && media?.tagName === "IMG") {
      media.dataset.srcReal = src;
      if (!media.getAttribute("src")) media.src = PLACEHOLDER;
      card.dataset.state = "unloaded";
    } else if (type === "video" && media?.tagName === "VIDEO") {
      media.dataset.srcReal = src;
      media.removeAttribute("src");
      media.preload = "none";
      card.dataset.state = "unloaded";
    }
  }

  function loadCard(card) {
    if (card.dataset.state === "loaded") return;

    const type = card.dataset.type;
    const media = card.querySelector("img, video");

    if (type === "image" && media?.tagName === "IMG") {
      media.src = media.dataset.srcReal || card.dataset.src;
      media.removeAttribute("data-src-real");
    } else if (type === "video" && media?.tagName === "VIDEO") {
      media.src = media.dataset.srcReal || card.dataset.src;
      media.preload = "metadata";
    }

    card.dataset.state = "loaded";
  }

  function unloadCard(card) {
    if (card.dataset.state !== "loaded") return;

    const type = card.dataset.type;
    const media = card.querySelector("img, video");

    if (type === "image" && media?.tagName === "IMG") {
      media.dataset.srcReal = media.src;
      media.src = PLACEHOLDER;
    } else if (type === "video" && media?.tagName === "VIDEO") {
      try {
        media.pause();
      } catch (_e) {
        /* ignore */
      }
      media.dataset.srcReal = media.src;
      media.removeAttribute("src");
      media.load();
      media.preload = "none";
    }

    card.dataset.state = "unloaded";
  }

  const ioLoad = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) loadCard(entry.target);
      }
    },
    { root: null, rootMargin: "1200px 0px", threshold: 0 }
  );

  const ioUnload = new IntersectionObserver(
    (entries) => {
      const vh = window.innerHeight || 800;

      for (const entry of entries) {
        if (entry.isIntersecting) continue;

        const rect = entry.boundingClientRect;
        const farAbove = rect.bottom < -2 * vh;
        const farBelow = rect.top > 3 * vh;

        if (farAbove || farBelow) unloadCard(entry.target);
      }
    },
    { root: null, rootMargin: "0px", threshold: 0 }
  );

  document.querySelectorAll(".card[data-type]").forEach((card) => {
    primeCard(card);
    ioLoad.observe(card);
    ioUnload.observe(card);
  });
})();

// === Equalize last row in .photos grid ===
(function() {
  const container = document.getElementById('gallery');
  if (!container) return;

  const LAST_CLASSES = ['_last-1','_last-2','_last-3'];

  function resetSpans() {
    container.querySelectorAll('.card').forEach(el => {
      LAST_CLASSES.forEach(c => el.classList.remove(c));
    });
  }

  function applyLastRowFill() {
    resetSpans();
    const cards = Array.from(container.querySelectorAll('.card'));
    if (cards.length === 0) return;

    // Group by visual rows via offsetTop (robust with CSS grid)
    const rows = [];
    cards.forEach(card => {
      const top = card.offsetTop;
      let row = rows.find(r => Math.abs(r.top - top) < 2);
      if (!row)
		  rows.push(row = { top, items: [] });
      row.items.push(card);
    });
	
	rows.forEach(row => {
		const n = row.items.length;
		if (n === 1)
			row.items.forEach(el => el.classList.add('_last-1'));
		else if (n === 2)
			row.items.forEach(el => el.classList.add('_last-2'));
		else if (n === 3)
			row.items.forEach(el => el.classList.add('_last-3'));
	});

    // const last = rows[rows.length - 1];
    // if (!last) return;

    // const n = last.items.length;
    // if (n === 1) last.items.forEach(el => el.classList.add('_last-1'));
    // else if (n === 2) last.items.forEach(el => el.classList.add('_last-2'));
    // else if (n === 3) last.items.forEach(el => el.classList.add('_last-3'));
    // 0 or 4+ — ничего не делаем: уже идеально занимает всю ширину
  }

  // Run on DOM ready and after images load/resize
  const ro = new ResizeObserver(applyLastRowFill);
  ro.observe(container);
  window.addEventListener('load', applyLastRowFill, { once: true });
  window.addEventListener('resize', applyLastRowFill);
  document.addEventListener('DOMContentLoaded', applyLastRowFill);
})();
