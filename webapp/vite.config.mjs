import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import mkcert from 'vite-plugin-mkcert';
import { fileURLToPath, URL } from 'node:url';
import Components from 'unplugin-vue-components/vite';
import { PrimeVueResolver } from 'unplugin-vue-components/resolvers';

const FRONTEND_PORT = 7210;
const API_PORT = 7211;

function strongholdPortGuard() {
    return {
        name: 'stronghold-port-guard',
        configResolved(config) {
            if (config.command !== 'serve') return;

            const actualPort = Number(config.server.port ?? FRONTEND_PORT);
            if (actualPort !== FRONTEND_PORT) {
                throw new Error(
                    `Stronghold frontend must run on ${FRONTEND_PORT}. ` +
                    `Port ${API_PORT} is reserved for the .NET API; refusing to start Vite on ${actualPort}.`
                );
            }
        },
    };
}

export default defineConfig(() => {
    return {
        build: { sourcemap: process.env.VITE_ENABLE_SOURCEMAP === 'true' },
        server: { port: FRONTEND_PORT, strictPort: true, https: true },
        plugins: [strongholdPortGuard(), vue(), mkcert(), Components({ resolvers: [PrimeVueResolver()] })],
        resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    };
});
