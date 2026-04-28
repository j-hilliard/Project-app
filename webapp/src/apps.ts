export const apps = {
    portal: {
        baseSlug: 'portal',
        name: 'Stronghold Platform',
        description: 'Platform home, summary dashboard, and application launcher',
        icon: 'pi pi-home',
        menu: {
            user: [],
            admin: [],
        },
    } as App,

    estimating: {
        baseSlug: 'estimating',
        name: 'Enterprise Estimating',
        description: 'Create and manage estimates, staffing plans, rate books, and cost analysis',
        icon: 'pi pi-calculator',
        menu: {
            user: [
                {
                    label: 'Estimates',
                    icon: 'pi pi-fw pi-list',
                    items: [
                        { label: 'Quote Log', icon: 'pi pi-fw pi-list', to: '/estimating/estimates' },
                        { label: 'New Estimate', icon: 'pi pi-fw pi-plus', to: '/estimating/estimates/new' },
                    ],
                },
                {
                    label: 'Staffing Plans',
                    icon: 'pi pi-fw pi-users',
                    items: [
                        { label: 'Staffing Plan Log', icon: 'pi pi-fw pi-users', to: '/estimating/staffing-plans' },
                        { label: 'New Staffing Plan', icon: 'pi pi-fw pi-user-plus', to: '/estimating/staffing-plans/new' },
                    ],
                },
                {
                    label: 'Library',
                    icon: 'pi pi-fw pi-book',
                    items: [
                        { label: 'Rate Books', icon: 'pi pi-fw pi-dollar', to: '/estimating/rate-books' },
                        { label: 'Cost Book', icon: 'pi pi-fw pi-chart-bar', to: '/estimating/cost-book' },
                        { label: 'Crew Templates', icon: 'pi pi-fw pi-sitemap', to: '/estimating/crew-templates' },
                    ],
                },
                {
                    label: 'Reports',
                    icon: 'pi pi-fw pi-chart-line',
                    items: [
                        { label: 'Revenue Forecast', icon: 'pi pi-fw pi-chart-bar', to: '/estimating/analytics/revenue' },
                        { label: 'Manpower Forecast', icon: 'pi pi-fw pi-users', to: '/estimating/analytics/manpower' },
                        { label: 'Calendar', icon: 'pi pi-fw pi-calendar', to: '/estimating/calendar' },
                    ],
                },
            ],
            admin: [],
        },
    } as App,

    planning: {
        baseSlug: 'planning',
        name: 'Field Planning',
        description: 'Step-out plans, work packages, FCOs, and execution sequencing',
        icon: 'pi pi-sitemap',
        menu: {
            user: [
                {
                    label: 'Plans',
                    icon: 'pi pi-fw pi-sitemap',
                    items: [
                        { label: 'Step-Out Plans', icon: 'pi pi-fw pi-list', to: '/planning/step-out-plans' },
                        { label: 'New Plan', icon: 'pi pi-fw pi-plus', to: '/planning/step-out-plans/new' },
                    ],
                },
                {
                    label: 'Work Packages',
                    icon: 'pi pi-fw pi-box',
                    items: [
                        { label: 'Work Package Log', icon: 'pi pi-fw pi-list', to: '/planning/work-packages' },
                    ],
                },
                {
                    label: 'Change Orders',
                    icon: 'pi pi-fw pi-file-edit',
                    items: [
                        { label: 'FCO / Change Orders', icon: 'pi pi-fw pi-file-edit', to: '/planning/fco' },
                    ],
                },
            ],
            admin: [],
        },
    } as App,

    scheduling: {
        baseSlug: 'scheduling',
        name: 'Scheduling',
        description: 'Resource assignments, coverage tracking, and roll-off planning',
        icon: 'pi pi-calendar',
        menu: {
            user: [
                {
                    label: 'Overview',
                    icon: 'pi pi-fw pi-chart-bar',
                    items: [
                        { label: 'Dashboard', icon: 'pi pi-fw pi-chart-bar', to: '/scheduling/dashboard' },
                    ],
                },
                {
                    label: 'Jobs & People',
                    icon: 'pi pi-fw pi-users',
                    items: [
                        { label: 'Jobs Board', icon: 'pi pi-fw pi-briefcase', to: '/scheduling/jobs' },
                        { label: 'Resources', icon: 'pi pi-fw pi-users', to: '/scheduling/resources' },
                        { label: 'Assignments', icon: 'pi pi-fw pi-calendar-plus', to: '/scheduling/assignments' },
                    ],
                },
                {
                    label: 'Coverage',
                    icon: 'pi pi-fw pi-exclamation-triangle',
                    items: [
                        { label: 'Craft Coverage', icon: 'pi pi-fw pi-chart-line', to: '/scheduling/coverage' },
                        { label: 'Ending Soon', icon: 'pi pi-fw pi-clock', to: '/scheduling/roll-off' },
                    ],
                },
            ],
            admin: [],
        },
    } as App,
};
