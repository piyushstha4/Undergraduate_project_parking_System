/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#eef7f2',
          100: '#d3ebe0',
          200: '#a6d7c1',
          300: '#79c3a2',
          400: '#4caf83',
          500: '#2f8f66',
          600: '#237250',
          700: '#1a5540',
          800: '#123a2d',
          900: '#0a221a',
        },
      },
    },
  },
  plugins: [],
}
