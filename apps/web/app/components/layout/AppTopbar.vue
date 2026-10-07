<script setup lang="ts">
import { Bell, ClipboardList, FolderKanban, LogOut, Menu, Search, UserRound, X } from '@lucide/vue'
import type { AnnouncementItem, LeaveItem, Member, Project, WorkItem } from '~/types/operations'
import { getInitials } from '~/utils/auth'

defineEmits<{ openNavigation: [] }>()
type SearchResult = { id: string, label: string, detail: string, path: string, kind: 'person' | 'project' | 'task' }

const auth = useAuth()
const { user } = auth
const { request } = useOperations()
const loggingOut = ref(false)
const searchInput = ref<HTMLInputElement>()
const query = ref('')
const searchResults = ref<SearchResult[]>([])
const searching = ref(false)
const searchOpen = ref(false)
const notificationsOpen = ref(false)
const notificationsLoading = ref(false)
const announcements = ref<AnnouncementItem[]>([])
const pendingLeave = ref<LeaveItem[]>([])
let searchTimer: ReturnType<typeof setTimeout> | undefined

watch(query, (value) => {
  clearTimeout(searchTimer)
  const normalized = value.trim().toLowerCase()
  if (normalized.length < 2) {
    searchResults.value = []
    searchOpen.value = false
    return
  }
  searching.value = true
  searchTimer = setTimeout(async () => {
    try {
      const [people, projects, tasks] = await Promise.all([
        request<Member[]>('people'), request<Project[]>('projects'), request<WorkItem[]>('tasks'),
      ])
      searchResults.value = [
        ...people.filter(item => `${item.name} ${item.email} ${item.jobTitle}`.toLowerCase().includes(normalized))
          .map(item => ({ id: item.id, label: item.name, detail: item.jobTitle || item.email, path: '/people', kind: 'person' as const })),
        ...projects.filter(item => `${item.name} ${item.description ?? ''}`.toLowerCase().includes(normalized))
          .map(item => ({ id: item.id, label: item.name, detail: 'Project', path: `/tasks?project=${item.id}`, kind: 'project' as const })),
        ...tasks.filter(item => `${item.title} ${item.description ?? ''} ${item.projectName}`.toLowerCase().includes(normalized))
          .map(item => ({ id: item.id, label: item.title, detail: item.projectName, path: `/tasks?project=${item.projectId}`, kind: 'task' as const })),
      ].slice(0, 8)
      searchOpen.value = true
    }
    catch {
      searchResults.value = []
      searchOpen.value = true
    }
    finally {
      searching.value = false
    }
  }, 250)
})

const openResult = async (result: SearchResult) => {
  searchOpen.value = false
  query.value = ''
  await navigateTo(result.path)
}

const toggleNotifications = async () => {
  notificationsOpen.value = !notificationsOpen.value
  searchOpen.value = false
  if (!notificationsOpen.value) return
  notificationsLoading.value = true
  try {
    const [latestAnnouncements, leave] = await Promise.all([
      request<AnnouncementItem[]>('announcements'), request<LeaveItem[]>('leave'),
    ])
    announcements.value = latestAnnouncements.slice(0, 4)
    pendingLeave.value = leave.filter(item => item.status === 'pending').slice(0, 4)
  }
  catch {
    announcements.value = []
    pendingLeave.value = []
  }
  finally {
    notificationsLoading.value = false
  }
}

const onShortcut = (event: KeyboardEvent) => {
  if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault()
    searchInput.value?.focus()
  }
  if (event.key === 'Escape') {
    searchOpen.value = false
    notificationsOpen.value = false
  }
}

onMounted(() => window.addEventListener('keydown', onShortcut))
onBeforeUnmount(() => {
  clearTimeout(searchTimer)
  window.removeEventListener('keydown', onShortcut)
})

const signOut = async () => {
  loggingOut.value = true
  try {
    await auth.logout()
    await navigateTo('/login')
  }
  finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <header class="topbar">
    <button class="icon-button topbar__menu" type="button" aria-label="Open navigation" aria-controls="primary-navigation" @click="$emit('openNavigation')"><Menu :size="21" /></button>
    <div class="topbar__mobile-brand">Workly</div>
    <div class="global-search-wrap">
      <label class="global-search">
        <Search :size="18" aria-hidden="true" />
        <span class="sr-only">Search workspace</span>
        <input ref="searchInput" v-model="query" type="search" placeholder="Search people, projects, and tasks" aria-controls="workspace-search-results" @focus="searchOpen = query.trim().length >= 2">
        <span v-if="searching" class="search-spinner" aria-label="Searching" />
        <kbd v-else>⌘ K</kbd>
      </label>
      <div v-if="searchOpen" id="workspace-search-results" class="topbar-popover search-results" role="listbox" aria-label="Workspace search results">
        <button v-for="result in searchResults" :key="`${result.kind}-${result.id}`" type="button" role="option" @click="openResult(result)">
          <UserRound v-if="result.kind === 'person'" :size="17" />
          <FolderKanban v-else-if="result.kind === 'project'" :size="17" />
          <ClipboardList v-else :size="17" />
          <span><strong>{{ result.label }}</strong><small>{{ result.detail }}</small></span>
        </button>
        <p v-if="!searching && searchResults.length === 0">No matching people, projects, or tasks.</p>
      </div>
    </div>
    <div class="topbar__actions">
      <div class="notification-menu">
        <button class="icon-button" type="button" aria-label="Notifications" :aria-expanded="notificationsOpen" @click="toggleNotifications"><Bell :size="20" /></button>
        <div v-if="notificationsOpen" class="topbar-popover notification-popover">
          <div class="popover-heading"><strong>Notifications</strong><button class="icon-button" type="button" aria-label="Close notifications" @click="notificationsOpen = false"><X :size="16" /></button></div>
          <p v-if="notificationsLoading">Loading updates…</p>
          <template v-else>
            <NuxtLink v-for="item in pendingLeave" :key="`leave-${item.id}`" to="/leave" @click="notificationsOpen = false"><ClipboardList :size="17" /><span><strong>Leave approval needed</strong><small>{{ item.memberName }} · {{ item.startDate }}</small></span></NuxtLink>
            <NuxtLink v-for="item in announcements" :key="`announcement-${item.id}`" to="/announcements" @click="notificationsOpen = false"><Bell :size="17" /><span><strong>{{ item.title }}</strong><small>Announcement by {{ item.authorName }}</small></span></NuxtLink>
            <p v-if="pendingLeave.length === 0 && announcements.length === 0">You're all caught up.</p>
          </template>
        </div>
      </div>
      <div class="user-menu">
        <AppAvatar :initials="getInitials(user?.displayName)" size="sm" tone="indigo" />
        <span class="user-menu__copy"><strong>{{ user?.displayName }}</strong><small>{{ user?.email }}</small></span>
        <button class="icon-button user-menu__logout" type="button" :disabled="loggingOut" aria-label="Sign out" @click="signOut"><LogOut :size="17" /></button>
      </div>
    </div>
  </header>
</template>
