/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './Views/**/*.cshtml',
    './Areas/**/*.cshtml',
    './Pages/**/*.cshtml',
    './wwwroot/js/**/*.js'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        khmer: ['"Kantumruy Pro"', 'system-ui', 'sans-serif'],
        display: ['"Playfair Display"', 'serif'],
      },
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        brand: {
          blue: '#4285F4',
          'blue-dark': '#3367D6',
          red: '#EA4335',
          green: '#34A853',
          yellow: '#FBBC05',
        },
        telegram: {
          DEFAULT: '#229ED9',
          hover: '#1b8bc2',
          dark: '#0d6fa0',
        },
      },
      boxShadow: {
        'card': '0 2px 12px rgba(60,64,67,.08)',
        'card-hover': '0 12px 32px rgba(60,64,67,.16)',
        'dropdown': '0 8px 32px rgba(0,0,0,.12)',
      },
    },
  },
  plugins: [],
}
