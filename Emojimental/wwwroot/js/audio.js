let bgm = null;

window.audioManager = {
    playBGM: function (url, volume) {
        if (!bgm) {
            bgm = new Audio(url);
            bgm.loop = true;
            bgm.volume = volume;

            // Handle autoplay restrictions
            const playAttempt = () => {
                bgm.play()
                    .then(() => {
                        window.removeEventListener('click', playAttempt);
                    })
                    .catch(e => console.log("Waiting for user interaction to play BGM..."));
            };

            window.addEventListener('click', playAttempt);
            playAttempt();
        } else {
            bgm.volume = volume;
        }
    },
    setVolume: function (volume) {
        if (bgm) {
            bgm.volume = volume;
        }
    }
};
