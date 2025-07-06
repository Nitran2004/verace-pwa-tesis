const CACHE_NAME = 'verace-v1.0.0';
const urlsToCache = [
  '/',
  '/css/site.css',
  '/js/site.js',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/manifest.json'
];

// Instalar Service Worker
self.addEventListener('install', function(event) {
  console.log('SW: Instalando Service Worker');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(function(cache) {
        console.log('SW: Cache abierto');
        return cache.addAll(urlsToCache);
      })
  );
});

// Activar Service Worker
self.addEventListener('activate', function(event) {
  console.log('SW: Service Worker activado');
  event.waitUntil(
    caches.keys().then(function(cacheNames) {
      return Promise.all(
        cacheNames.map(function(cacheName) {
          if (cacheName !== CACHE_NAME) {
            console.log('SW: Eliminando cache antiguo:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
});

// Interceptar requests
self.addEventListener('fetch', function(event) {
  // Solo cachear requests GET
  if (event.request.method !== 'GET') {
    return;
  }

  // No cachear requests de API que modifican datos
  if (event.request.url.includes('/api/') || 
      event.request.url.includes('AgregarAlCarrito') ||
      event.request.url.includes('ProcesarPedido') ||
      event.request.url.includes('ActualizarCarrito')) {
    return;
  }

  event.respondWith(
    caches.match(event.request)
      .then(function(response) {
        // Devolver del cache si existe
        if (response) {
          return response;
        }

        // Si no está en cache, hacer request normal
        return fetch(event.request).then(function(response) {
          // Verificar que la respuesta sea válida
          if (!response || response.status !== 200 || response.type !== 'basic') {
            return response;
          }

          // Clonar la respuesta para cache
          var responseToCache = response.clone();

          caches.open(CACHE_NAME)
            .then(function(cache) {
              cache.put(event.request, responseToCache);
            });

          return response;
        });
      })
  );
});

// Manejar cuando la app está offline
self.addEventListener('fetch', function(event) {
  if (event.request.destination === 'document') {
    event.respondWith(
      caches.match(event.request).then(function(response) {
        return response || caches.match('/');
      })
    );
  }
});