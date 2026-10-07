<template>
  <main class="auth-layout">
    <section class="auth-layout__intro" aria-labelledby="auth-brand-title">
      <NuxtLink to="/" class="brand auth-layout__brand" aria-label="Workly home">
        <span class="brand__mark" aria-hidden="true"><span /><span /><span /></span>
        <span class="brand__name">Workly</span>
      </NuxtLink>
      <div>
        <p class="auth-layout__eyebrow">People and work, together</p>
        <h1 id="auth-brand-title">One calm workspace for your whole team.</h1>
        <p>Plan projects, support your people, and keep daily operations moving without losing the human context.</p>
      </div>
      <p class="auth-layout__note">Secure sessions · Short-lived access · Refresh token rotation</p>
    </section>
    <section class="auth-layout__content">
      <slot />
    </section>
  </main>
</template>
