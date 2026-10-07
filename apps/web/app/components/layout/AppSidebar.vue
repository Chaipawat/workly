<script setup lang="ts">
import { FolderKanban, LayoutDashboard, ListTodo, Megaphone, Network, Palmtree, Settings, Users, X } from '@lucide/vue'
import { getInitials } from '~/utils/auth'

defineProps<{ open: boolean }>()
defineEmits<{ close: [] }>()

const { user } = useAuth()

const navigation = [
  { label: 'Dashboard', to: '/dashboard', icon: LayoutDashboard },
  { label: 'People', to: '/people', icon: Users },
  { label: 'Teams', to: '/teams', icon: Network },
  { label: 'Projects', to: '/projects', icon: FolderKanban },
  { label: 'Tasks', to: '/tasks', icon: ListTodo },
  { label: 'Leave', to: '/leave', icon: Palmtree },
  { label: 'Announcements', to: '/announcements', icon: Megaphone },
]
</script>

<template>
  <aside id="primary-navigation" class="sidebar" :class="{ 'sidebar--open': open }">
    <div class="sidebar__brand-row">
      <NuxtLink to="/dashboard" class="brand" aria-label="Workly dashboard" @click="$emit('close')">
        <span class="brand__mark" aria-hidden="true"><span /><span /><span /></span>
        <span class="brand__name">Workly</span>
      </NuxtLink>
      <button class="icon-button sidebar__close" type="button" aria-label="Close navigation" @click="$emit('close')"><X :size="20" /></button>
    </div>

    <OrganizationSwitcher class="sidebar__org" />

    <nav class="sidebar__nav" aria-label="Primary navigation">
      <span class="sidebar__eyebrow">Workspace</span>
      <NuxtLink v-for="item in navigation" :key="item.label" :to="item.to" class="nav-link" @click="$emit('close')">
        <component :is="item.icon" :size="19" :stroke-width="1.9" aria-hidden="true" /><span>{{ item.label }}</span>
      </NuxtLink>
    </nav>

    <div class="sidebar__footer">
      <NuxtLink to="/settings/organization" class="nav-link" @click="$emit('close')"><Settings :size="19" aria-hidden="true" /><span>Settings</span></NuxtLink>
      <div class="sidebar__profile">
        <AppAvatar :initials="getInitials(user?.displayName)" size="sm" tone="indigo" />
        <span><strong>{{ user?.displayName }}</strong><small>Member</small></span>
        <span class="presence-dot" aria-label="Online" />
      </div>
    </div>
  </aside>
  <button v-if="open" class="sidebar-backdrop" type="button" aria-label="Close navigation" @click="$emit('close')" />
</template>
