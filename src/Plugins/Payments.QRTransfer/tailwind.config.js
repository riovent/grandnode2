/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.cshtml", // Template dosyalarınızın yolunu belirtin
    ],
    important: true
}

//NPM init create library
//npm init

//NOT INITIALIZE
//npm install @tailwindcss/cli tailwindcss -D

//RUN CODE
//npx @tailwindcss/cli -i ./Content/css/app.css -o ./Content/css/tailwind.css --watch