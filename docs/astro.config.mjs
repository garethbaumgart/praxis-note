// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
	integrations: [
		starlight({
			title: 'PraxisNote Docs',
			logo: {
				light: './src/assets/logo-light.svg',
				dark: './src/assets/logo-dark.svg',
				replacesTitle: false,
			},
			social: [
				{ icon: 'github', label: 'GitHub', href: 'https://github.com/garethbaumgart/praxis-note' },
			],
			sidebar: [
				{ label: 'Welcome', slug: '' },
				{ label: 'Quick Tour', slug: 'quick-tour' },
				{
					label: 'Features',
					items: [
						{ label: 'Tasks', slug: 'tasks' },
						{ label: 'Notes', slug: 'notes' },
						{ label: 'Meetings', slug: 'meetings' },
						{ label: 'Tags & Tag Hub', slug: 'tags' },
						{ label: 'Insights', slug: 'insights' },
						{ label: 'Profiles', slug: 'profiles' },
						{ label: 'Integrations', slug: 'integrations' },
					],
				},
				{
					label: 'Reference',
					items: [
						{ label: 'Keyboard Shortcuts', slug: 'keyboard-shortcuts' },
						{ label: 'FAQ', slug: 'faq' },
					],
				},
			],
			customCss: ['./src/styles/custom.css'],
			editLink: {
				baseUrl: 'https://github.com/garethbaumgart/praxis-note/edit/main/docs/',
			},
			pagefind: true,
		}),
	],
});
