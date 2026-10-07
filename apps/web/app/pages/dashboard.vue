<script setup lang="ts">
import { Plus, UserPlus } from '@lucide/vue'

definePageMeta({ middleware: 'auth' })

const { data, state } = useDashboard()
const { user } = useAuth()
const today = new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'long', day: 'numeric' }).format(new Date())
</script>

<template>
  <div class="dashboard-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ data.organizationName }} · Overview</p>
        <h1>Good morning, {{ user?.displayName }} <span aria-hidden="true">👋</span></h1>
        <p>Here’s what’s happening at {{ data.organizationName }} today.</p>
      </div>
      <div class="page-header__actions">
        <time :datetime="new Date().toISOString().slice(0, 10)">{{ today }}</time>
        <NuxtLink class="button button--secondary" to="/people">
          <UserPlus :size="17" /> Add members
        </NuxtLink>
        <NuxtLink class="button button--primary" to="/projects">
          <Plus :size="17" /> Create project
        </NuxtLink>
      </div>
    </header>

    <DashboardLoading v-if="state === 'loading'" />
    <DashboardError v-else-if="state === 'error'" />
    <DashboardEmpty v-else-if="state === 'empty'" />
    <template v-else>
      <DashboardKpis :metrics="data.metrics" />
      <div class="dashboard-grid dashboard-grid--primary">
        <ActiveProjects :projects="data.projects" />
        <OnLeaveToday :people="data.onLeaveToday" />
        <UpcomingList :items="data.upcoming" />
      </div>
      <div class="dashboard-grid dashboard-grid--secondary">
        <TaskStatusOverview :status="data.taskStatus" />
        <TeamOverview :members="data.team" />
        <RecentActivity :activities="data.activities" />
      </div>
    </template>
  </div>
</template>
