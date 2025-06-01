import leoProfanity from "leo-profanity";

window.profanityFilter = {
    cleanText: (text) => {
        return leoProfanity.clean(text);
    },
    addWords: (words) => {
        leoProfanity.add(words);
    },
    removeWords: (words) => {
        leoProfanity.remove(words);
    }
};