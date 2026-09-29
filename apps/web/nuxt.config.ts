export default defineNuxtConfig({
  compatibilityDate: '2026-09-01',
  devtools: { enabled: true },
  modules: ['@nuxt/eslint'],
  components: [{ path: '~/components', pathPrefix: false }],
  css: ['@fontsource-variable/inter', '~/assets/css/main.css'],
  app: {
    head: {
      title: 'Workly',
      htmlAttrs: { lang: 'en' },
      meta: [
        { name: 'description', content: 'One workspace for your team to manage people, work, and daily operations.' },
        { name: 'theme-color', content: '#f8fafc' },
      ],
    },
  },
  typescript: { strict: true, typeCheck: true },
})
