<script setup lang="ts">
const auth = useAuth()
await auth.initialize()
await navigateTo(auth.isAuthenticated.value ? '/dashboard' : '/login', { replace: true })
</script>
