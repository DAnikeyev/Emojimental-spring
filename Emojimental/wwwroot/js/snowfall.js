window.emojimentalSnowfall = {
    canvas: null,
    ctx: null,
    flakes: [],
    raindrops: [],
    energy: [],
    starParticles: [],
    snowIntensity: 0,
    rainIntensity: 0,
    energyIntensity: 0,
    sunX: 90,
    sunY: 5,
    lastUpdate: 0,
    requestId: null,
    snowEmoji: "❄️",
    rainEmoji: "💧",
    energyEmoji: "⚡",

    init(canvas, snowEmoji, rainEmoji, energyEmoji) {
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
        this.snowEmoji = snowEmoji || "❄️";
        this.rainEmoji = rainEmoji || "💧";
        this.energyEmoji = energyEmoji || "⚡";
        this.resize();
        window.addEventListener('resize', () => this.resize());
        this.lastUpdate = performance.now();
        this.loop();
    },

    resize() {
        if (this.canvas) {
            this.canvas.width = this.canvas.clientWidth;
            this.canvas.height = this.canvas.clientHeight;
        }
        this.computeSunPosition();
    },

    setSnowIntensity(intensity) {
        this.snowIntensity = Math.max(0, Math.min(1, intensity));
    },

    setRainIntensity(intensity) {
        this.rainIntensity = Math.max(0, Math.min(1, intensity));
    },

    setEnergyIntensity(intensity, sunX, sunY) {
        this.energyIntensity = Math.max(0, Math.min(1, intensity));
        if (sunX !== undefined && sunY !== undefined) {
            this.sunX = sunX;
            this.sunY = sunY;
        } else {
            this.computeSunPosition();
        }
    },

    computeSunPosition() {
        // Look for the sun/moon container inside the field surface
        const container = document.querySelector('.field-celestial-container');
        if (!container) return;
        const rect = container.getBoundingClientRect();
        this.sunX = ((rect.left + rect.width / 2) / window.innerWidth) * 100;
        this.sunY = ((rect.top + rect.height / 2) / window.innerHeight) * 100;
    },

    loop(now) {
        const dt = Math.min((now - this.lastUpdate) / 1000, 0.1);
        this.lastUpdate = now;

        this.updateSnow(dt);
        this.updateRain(dt);
        this.updateEnergy(dt);
        this.updateStars(dt);
        this.draw();

        this.requestId = requestAnimationFrame((n) => this.loop(n));
    },

    updateSnow(dt) {
        for (let i = this.flakes.length - 1; i >= 0; i--) {
            const flake = this.flakes[i];
            flake.y += flake.speed * dt;
            flake.phase += flake.phaseSpeed * dt;
            flake.horizontalOffset = Math.sin(flake.phase) * flake.amplitude;

            if (flake.y > 110) {
                this.flakes.splice(i, 1);
            }
        }

        const targetCount = Math.floor(this.snowIntensity * 100);
        if (this.flakes.length < targetCount && Math.random() < this.snowIntensity * 0.5) {
            this.flakes.push({
                x: Math.random() * 100,
                y: -10,
                speed: 5 + Math.random() * 15,
                size: 0.8 + Math.random() * 1.2,
                phase: Math.random() * Math.PI * 2,
                phaseSpeed: 1 + Math.random() * 2,
                amplitude: 10 + Math.random() * 20,
                horizontalOffset: 0
            });
        }
    },

    updateRain(dt) {
        for (let i = this.raindrops.length - 1; i >= 0; i--) {
            const drop = this.raindrops[i];
            drop.y += drop.speed * dt;
            drop.x += drop.wind * dt;

            if (drop.y > 110) {
                this.raindrops.splice(i, 1);
            }
        }

        const targetCount = Math.floor(this.rainIntensity * 150);
        if (this.raindrops.length < targetCount && Math.random() < this.rainIntensity * 0.7) {
            this.raindrops.push({
                x: Math.random() * 110 - 5,
                y: -10 - Math.random() * 20,
                speed: 40 + Math.random() * 60,
                wind: -5 + Math.random() * 10,
                size: 0.6 + Math.random() * 1.0
            });
        }
    },

    updateEnergy(dt) {
        for (let i = this.energy.length - 1; i >= 0; i--) {
            const part = this.energy[i];
            part.x += part.vx * dt;
            part.y += part.vy * dt;
            part.life -= dt;

            if (part.life <= 0) {
                this.energy.splice(i, 1);
            }
        }

        const targetCount = Math.floor(this.energyIntensity * 50);
        if (this.energy.length < targetCount && Math.random() < this.energyIntensity * 0.5) {
            // Emit from sun position
            const angle = Math.PI * 0.5 + (Math.random() - 0.5) * Math.PI * 0.6;
            const speed = 20 + Math.random() * 30;
            this.energy.push({
                x: this.sunX + (Math.random() - 0.5) * 5,
                y: this.sunY + (Math.random() - 0.5) * 5,
                vx: -Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                size: 0.8 + Math.random() * 0.8,
                life: 2 + Math.random() * 2
            });
        }
    },

    updateStars(dt) {
        for (let i = this.starParticles.length - 1; i >= 0; i--) {
            const p = this.starParticles[i];
            p.x += p.vx * dt;
            p.y += p.vy * dt;
            p.life -= dt;
            if (p.life <= 0) this.starParticles.splice(i, 1);
        }
    },

    addStarTrail(x, y) {
        for (let i = 0; i < 2; i++) {
            this.starParticles.push({
                x: x,
                y: y,
                vx: (Math.random() - 0.5) * 10,
                vy: (Math.random() - 0.5) * 10,
                life: 0.5 + Math.random() * 0.5,
                size: 0.5 + Math.random() * 0.5,
                emoji: "✨"
            });
        }
    },

    addStarExplosion(x, y) {
        for (let i = 0; i < 20; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = 20 + Math.random() * 40;
            this.starParticles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                life: 1 + Math.random() * 1,
                size: 0.8 + Math.random() * 1.2,
                emoji: Math.random() > 0.5 ? "⭐" : "✨"
            });
        }
    },

    draw() {
        if (!this.ctx || !this.canvas) return;

        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        const w = this.canvas.width;
        const h = this.canvas.height;
        const baseFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;

        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';

        // Draw energy
        for (const part of this.energy) {
            const fontSize = part.size * baseFontSize;
            this.ctx.font = `${fontSize}px serif`;
            const realX = (part.x / 100) * w;
            const realY = (part.y / 100) * h;
            this.ctx.globalAlpha = Math.min(1, part.life);
            this.ctx.fillText(this.energyEmoji, realX, realY);
        }
        this.ctx.globalAlpha = 1;

        // Draw star particles
        for (const p of this.starParticles) {
            const fontSize = p.size * baseFontSize;
            this.ctx.font = `${fontSize}px serif`;
            const realX = (p.x / 100) * w;
            const realY = (p.y / 100) * h;
            this.ctx.globalAlpha = Math.min(1, p.life);
            this.ctx.fillText(p.emoji, realX, realY);
        }
        this.ctx.globalAlpha = 1;

        // Draw snowflakes
        for (const flake of this.flakes) {
            const fontSize = flake.size * baseFontSize;
            this.ctx.font = `${fontSize}px serif`;

            const realX = (flake.x / 100) * w + flake.horizontalOffset;
            const realY = (flake.y / 100) * h;

            this.ctx.fillText(this.snowEmoji, realX, realY);
        }

        // Draw raindrops
        for (const drop of this.raindrops) {
            const fontSize = drop.size * baseFontSize;
            this.ctx.font = `${fontSize}px serif`;

            const realX = (drop.x / 100) * w;
            const realY = (drop.y / 100) * h;

            this.ctx.globalAlpha = 0.7;
            this.ctx.fillText(this.rainEmoji, realX, realY);
            this.ctx.globalAlpha = 1;
        }
    },

    dispose() {
        if (this.requestId) {
            cancelAnimationFrame(this.requestId);
        }
        window.removeEventListener('resize', () => this.resize());
    }
};
