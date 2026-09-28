/**
 * FitNet - Aplicación Vue 3 para Landing Page
 * Gestiona:
 * 1. Filtro interactivo de disciplinas y entrenamientos
 * 2. Selector dinámico de planes y membresías (mensual / semestral)
 * 3. Calculadora reactiva de IMC y asesoramiento fitness personalizado
 */

const { createApp, ref, computed } = Vue;

createApp({
  setup() {
    // ==========================================
    // 1. Selector de Planes y Membresías
    // ==========================================
    const periodo = ref('mensual'); // 'mensual' | 'semestral'

    const planes = [
      {
        id: 'free',
        nombre: 'Free Trial',
        badge: 'Prueba de 24 Horas',
        subtitulo: 'Conocé la plataforma sin costo durante las primeras 24 horas.',
        precioMensual: 0,
        precioSemestral: 0,
        destacado: false,
        btnTexto: 'Comenzar Prueba Gratis',
        btnClase: 'btn-fn-outline',
        caracteristicas: [
          { texto: 'Acceso total por 24 horas', incluida: true },
          { texto: 'Perfil digital y avatar', incluida: true },
          { texto: 'Catálogo de ejercicios públicos', incluida: true },
          { texto: 'Rutinas guiadas del gimnasio', incluida: false },
          { texto: 'Registro y fotos de progreso', incluida: false },
          { texto: 'Entrenador personal asignado', incluida: false }
        ]
      },
      {
        id: 'gold',
        nombre: 'Plan Gold',
        badge: 'Más Elegido',
        subtitulo: 'Para quienes entrenan de forma independiente y constante.',
        precioMensual: 32000,
        precioSemestral: 25600, // 20% off (32.000 * 0.8)
        destacado: true,
        btnTexto: 'Elegir Plan Gold',
        btnClase: 'btn-fn-primary',
        caracteristicas: [
          { texto: 'Acceso ilimitado sin límite de tiempo', incluida: true },
          { texto: 'Biblioteca completa de ejercicios (técnica y series)', incluida: true },
          { texto: 'Rutinas oficiales del gimnasio (fuerza, hipertrofia, resistencia)', incluida: true },
          { texto: 'Registro continuo de progreso y comparativa fotográfica', incluida: true },
          { texto: 'Soporte y atención administrativa', incluida: true },
          { texto: 'Entrenador personal asignado', incluida: false }
        ]
      },
      {
        id: 'premium',
        nombre: 'Plan Premium',
        badge: 'Máximo Nivel',
        subtitulo: 'Acompañamiento profesional 1 a 1 y planificación a medida.',
        precioMensual: 65000,
        precioSemestral: 52000, // 20% off (65.000 * 0.8)
        destacado: false,
        btnTexto: 'Elegir Plan Premium',
        btnClase: 'btn-fn-outline',
        caracteristicas: [
          { texto: 'Todo lo incluido en el Plan Gold', incluida: true },
          { texto: 'Profesor / Entrenador personal asignado', incluida: true },
          { texto: 'Rutinas 100% individualizadas según objetivos', incluida: true },
          { texto: 'Seguimiento individual de cargas, peso y evolución', incluida: true },
          { texto: 'Observaciones técnicas y feedback de tu entrenador', incluida: true },
          { texto: 'Ajuste dinámico de series y repeticiones', incluida: true }
        ]
      }
    ];

    function formatearMoneda(valor) {
      if (valor === 0) return 'GRATIS';
      return '$' + valor.toLocaleString('es-AR');
    }

    // ==========================================
    // 2. Filtro Interactivo de Disciplinas
    // ==========================================
    const categoriaActiva = ref('todos');

    const categorias = [
      { id: 'todos', nombre: 'Todas las Disciplinas' },
      { id: 'musculacion', nombre: 'Musculación' },
      { id: 'crossfit', nombre: 'CrossFit & WOD' },
      { id: 'funcional', nombre: 'Funcional' },
      { id: 'cardio', nombre: 'Cardio & HIIT' }
    ];

    const disciplinas = [
      {
        id: 1,
        categoria: 'musculacion',
        nombre: 'Hipertrofia & Fuerza',
        intensidad: 'Alta',
        imagen: '/img/gallery/cat1.png',
        descripcion: 'Entrenamiento guiado con pesas libres, máquinas biomecánicas y control estricto de sobrecarga progresiva.'
      },
      {
        id: 2,
        categoria: 'crossfit',
        nombre: 'Cross Training & WOD',
        intensidad: 'Muy Alta',
        imagen: '/img/gallery/cat2.png',
        descripcion: 'Ejercicios de alta intensidad combinando levantamiento olímpico, gimnasia deportiva y resistencia cardiovascular.'
      },
      {
        id: 3,
        categoria: 'funcional',
        nombre: 'Entrenamiento Funcional',
        intensidad: 'Media / Alta',
        imagen: '/img/gallery/about.png',
        descripcion: 'Movimientos multiarticulares con peso corporal, kettlebells y bandas elásticas para mejorar postura y agilidad.'
      },
      {
        id: 4,
        categoria: 'cardio',
        nombre: 'HIIT & Resistencia',
        intensidad: 'Alta',
        imagen: '/img/gallery/gallery2.png',
        descripcion: 'Intervalos de alta intensidad diseñados para maximizar quema calórica, capacidad pulmonar y salud cardiovascular.'
      },
      {
        id: 5,
        categoria: 'musculacion',
        nombre: 'Potencia & Core',
        intensidad: 'Media',
        imagen: '/img/gallery/gallery3.png',
        descripcion: 'Rutinas enfocadas en la zona media, estabilidad lumbar y levantamientos básicos fundamentales (sentadilla, peso muerto).'
      },
      {
        id: 6,
        categoria: 'cardio',
        nombre: 'Spinning & Indoor Bike',
        intensidad: 'Media / Alta',
        imagen: '/img/gallery/gallery4.png',
        descripcion: 'Sesiones de ciclismo de interior con simulación de pendientes, cadencia rítmica y control de frecuencia cardíaca.'
      }
    ];

    const disciplinasFiltradas = computed(() => {
      if (categoriaActiva.value === 'todos') {
        return disciplinas;
      }
      return disciplinas.filter(d => d.categoria === categoriaActiva.value);
    });

    // ==========================================
    // 3. Calculadora de IMC & Diagnóstico Fitness
    // ==========================================
    const altura = ref(175); // cm
    const peso = ref(74);    // kg
    const edad = ref(26);
    const objetivo = ref('musculo'); // 'musculo' | 'peso' | 'salud'

    const imc = computed(() => {
      const hMetros = altura.value / 100;
      if (!hMetros || hMetros <= 0) return 0;
      const valor = peso.value / (hMetros * hMetros);
      return Math.round(valor * 10) / 10;
    });

    const diagnosticoImc = computed(() => {
      const v = imc.value;
      if (v < 18.5) {
        return {
          categoria: 'Bajo Peso',
          claseColor: 'text-warning',
          barraColor: 'bg-warning',
          progreso: 25,
          consejo: 'Te recomendamos un plan enfocado en superávit calórico y entrenamiento de hipertrofia para ganar masa magra de forma saludable.',
          planSugerido: 'Plan Gold (Rutinas de Hipertrofia)'
        };
      } else if (v >= 18.5 && v < 25) {
        return {
          categoria: 'Peso Saludable / Óptimo',
          claseColor: 'text-primary-fn',
          barraColor: 'bg-primary-fn',
          progreso: 50,
          consejo: '¡Excelente condición inicial! Podés orientarte al desarrollo de fuerza máxima, resistencia o definición muscular según tu meta personal.',
          planSugerido: 'Plan Gold o Premium'
        };
      } else if (v >= 25 && v < 30) {
        return {
          categoria: 'Sobrepeso Grado I',
          claseColor: 'text-warning',
          barraColor: 'bg-warning',
          progreso: 75,
          consejo: 'Combinar rutinas de fuerza con bloques de HIIT y reordenamiento nutricional te permitirá recomponer masa muscular y descender tejido adiposo.',
          planSugerido: 'Plan Premium (Seguimiento Personalizado)'
        };
      } else {
        return {
          categoria: 'Obesidad / Requiere Cuidado',
          claseColor: 'text-danger',
          barraColor: 'bg-danger',
          progreso: 100,
          consejo: 'Es fundamental avanzar con un plan progresivo de bajo impacto articular, supervisado por un profesional para garantizar una evolución segura.',
          planSugerido: 'Plan Premium (Acompañamiento 1 a 1)'
        };
      }
    });

    // ==========================================
    // 4. Preguntas Frecuentes (FAQ)
    // ==========================================
    const faqActivo = ref(1);

    const faqs = [
      {
        id: 1,
        pregunta: '¿Cómo funciona la prueba gratuita de 24 horas?',
        respuesta: 'Al registrarte con tu correo electrónico accedés automáticamente al Plan Free sin costo. Durante las primeras 24 horas podés navegar tu perfil, explorar el catálogo público de ejercicios y familiarizarte con el entorno. Cumplidas las 24 horas, tu cuenta solicita el pase a Gold o Premium para acceder a rutinas y seguimiento.'
      },
      {
        id: 2,
        pregunta: '¿Cuál es la diferencia entre el Plan Gold y el Plan Premium?',
        respuesta: 'El Plan Gold te brinda acceso ilimitado a toda la biblioteca de ejercicios, rutinas oficiales del gimnasio y registro fotográfico de tu evolución. El Plan Premium incluye todo eso más un entrenador personal asignado que diseña rutinas a tu medida y realiza un seguimiento periódico de tus métricas.'
      },
      {
        id: 3,
        pregunta: '¿Puedo subir fotos de mi progreso físico?',
        respuesta: 'Sí, tanto los socios Gold como Premium cuentan con el módulo "Mi Progreso", donde pueden cargar periódicamente su peso, porcentaje de grasa, notas y fotografías de evolución para comparar su transformación física a lo largo del tiempo.'
      },
      {
        id: 4,
        pregunta: '¿Cómo obtengo mi rutina personalizada si elijo Premium?',
        respuesta: 'Una vez asignado tu entrenador personal en el sistema, él elaborará tu plan de entrenamiento específico (series, repeticiones, descansos y notas técnicas). Podrás consultarlo y registrar tu cumplimiento directamente desde tu teléfono o computadora.'
      }
    ];

    function toggleFaq(id) {
      faqActivo.value = faqActivo.value === id ? null : id;
    }

    return {
      periodo,
      planes,
      formatearMoneda,
      categoriaActiva,
      categorias,
      disciplinas,
      disciplinasFiltradas,
      altura,
      peso,
      edad,
      objetivo,
      imc,
      diagnosticoImc,
      faqActivo,
      faqs,
      toggleFaq
    };
  }
}).mount('#fitnet-landing-app');
