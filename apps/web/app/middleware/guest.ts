export default defineNuxtRouteMiddleware(async () => {
  const auth = useAuth()
  await auth.initialize()
  if (auth.isAuthenticated.value) return navigateTo('/dashboard')
})
