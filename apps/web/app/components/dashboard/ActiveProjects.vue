<script setup lang="ts">
import { ArrowRight } from '@lucide/vue'
import type { DashboardProject } from '~/types/dashboard'
import { getProjectProgress } from '~/utils/dashboard'

defineProps<{ projects: DashboardProject[] }>()
</script>

<template>
  <section class="card panel panel--projects">
    <div class="panel__header">
      <div>
        <h2>Active projects</h2>
        <p>Projects moving across your organization</p>
      </div>
      <NuxtLink to="/projects" class="text-link">View all
        <ArrowRight :size="15" />
      </NuxtLink>
    </div>
    <div class="project-list">
      <article v-for="project in projects" :key="project.id" class="project-row">
        <div class="project-row__heading">
          <span class="project-accent" :class="`tone-${project.accent}`" aria-hidden="true" />
          <div>
            <h3>{{ project.name }}</h3>
            <p>{{ project.description }}</p>
          </div>
        </div>
        <div class="project-row__progress">
          <div class="progress-copy"><span>{{ getProjectProgress(project) }}%</span><small>{{ project.completedTasks }}
              / {{ project.totalTasks }} tasks</small></div>
          <div class="progress-track" role="progressbar" :aria-label="`${project.name} progress`"
            :aria-valuenow="getProjectProgress(project)" aria-valuemin="0" aria-valuemax="100"><span
              :class="`tone-${project.accent}`" :style="{ width: `${getProjectProgress(project)}%` }" /></div>
        </div>
        <div class="project-row__meta">
          <div class="avatar-stack" :aria-label="`${project.memberInitials.length} project members`">
            <AppAvatar v-for="initials in project.memberInitials" :key="initials" :initials="initials" size="sm"
              tone="slate" />
          </div>
          <small>{{ project.dueLabel }}</small>
        </div>
      </article>
    </div>
  </section>
</template>
