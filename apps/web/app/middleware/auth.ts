export default defineNuxtRouteMiddleware(async (to) => {
  const auth = useAuth()
  await auth.initialize()
  if (!auth.isAuthenticated.value) {
    return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
  }
})
