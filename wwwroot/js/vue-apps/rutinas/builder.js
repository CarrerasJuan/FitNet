/**
 * FitNet - Constructor y Diseñador Interactivo de Rutinas (Fase 9 - Tareas 9.2 y 9.3)
 * Stack: Vue 3 Composition API (Global Build) + Axios Local
 * Montaje: <div id="routine-builder">
 * 
 * Flujo de Trabajo Deportivo:
 * 1. Selección directa de Alumno destinatario o Plantilla reutilizable.
 * 2. Navegación visual por Zonas del Cuerpo (Pecho, Espalda, Piernas, etc.) con buscador instantáneo.
 * 3. Prescripción técnica de cargas: Series, Repeticiones, Descanso y Notas (Requisito Cátedra REQ-01 / REQ-06).
 * 4. Guardado transaccional y asignación directa en un solo paso.
 */

const { createApp, ref, reactive, computed, onMounted } = Vue;

const routineApp = createApp({
  setup() {
    // 1. Datos iniciales inyectados desde el servidor Razor
    const isEditing = ref(window.__IS_EDITING__ || false);
    const availableMembers = ref(window.__AVAILABLE_MEMBERS__ || []);
    const selectedMemberId = ref(Number(window.__PRESELECTED_MEMBER_ID__ || 0));
    const routineName = ref(window.__INITIAL_NAME__ || '');
    const routineDescription = ref(window.__INITIAL_DESCRIPTION__ || '');

    // 2. Estado de navegación por Zonas del Cuerpo y Catálogo
    const zones = [
      { id: 'all', name: 'Todas las Zonas', icon: 'fa-solid fa-layer-group' },
      { id: 'Pecho', name: 'Pecho', icon: 'fa-solid fa-shield-halved' },
      { id: 'Espalda', name: 'Espalda', icon: 'fa-solid fa-mountain' },
      { id: 'Piernas', name: 'Piernas', icon: 'fa-solid fa-person-walking' },
      { id: 'Hombros', name: 'Hombros', icon: 'fa-solid fa-dumbbell' },
      { id: 'Brazos', name: 'Brazos', icon: 'fa-solid fa-hand-fist' },
      { id: 'Core', name: 'Core', icon: 'fa-solid fa-bullseye' }
    ];

    const activeZone = ref('Pecho'); // Inicia mostrando ejercicios de Pecho por defecto
    const searchQuery = ref('');
    const catalogExercises = ref([]);
    const isLoadingCatalog = ref(false);
    let searchTimeout = null;

    // 3. Lista reactiva de ejercicios seleccionados para la rutina
    const initialExercises = window.__INITIAL_EXERCISES__ || [];
    const selectedExercises = ref(
      initialExercises.map((e, index) => ({
        exerciseId: e.exerciseId || e.ExerciseId,
        exerciseName: e.exerciseName || e.ExerciseName || 'Ejercicio',
        muscleGroup: e.muscleGroup || e.MuscleGroup || 'General',
        difficulty: e.difficulty || e.Difficulty || 'Intermedio',
        imagePath: e.imagePath || e.ImagePath || null,
        order: e.order || e.Order || index + 1,
        sets: e.sets || e.Sets || 3,
        repetitions: e.repetitions || e.Repetitions || 10,
        restSeconds: e.restSeconds || e.RestSeconds || 60,
        notes: e.notes || e.Notes || ''
      }))
    );

    // 4. Notificación flotante / Toast rápido
    const notification = reactive({
      visible: false,
      message: '',
      type: 'success'
    });

    const showNotification = (message, type = 'success') => {
      notification.message = message;
      notification.type = type;
      notification.visible = true;
      setTimeout(() => {
        notification.visible = false;
      }, 3500);
    };

    // 5. Cómputos del Alumno Seleccionado
    const currentMember = computed(() => {
      if (!selectedMemberId.value || selectedMemberId.value === 0) return null;
      return availableMembers.value.find(m => m.id === Number(selectedMemberId.value)) || null;
    });

    const selectedMemberName = computed(() => currentMember.value?.fullName || '');
    const selectedMemberEmail = computed(() => currentMember.value?.email || '');
    const selectedMemberAvatar = computed(() => currentMember.value?.avatarPath || '');

    const onMemberChanged = () => {
      const member = currentMember.value;
      if (member) {
        showNotification(`Alumno ${member.fullName} seleccionado para asignación directa.`, 'info');
      }
    };

    // Placeholders limpios y contextuales (sin forzar texto editable en el input)
    const computedNamePlaceholder = computed(() => {
      if (currentMember.value) {
        return `ej: Hipertrofia Pecho y Brazos (${currentMember.value.fullName})`;
      }
      return 'ej: Hipertrofia Torso, Fuerza 5x5, Acondicionamiento...';
    });

    const computedDescPlaceholder = computed(() => {
      if (currentMember.value) {
        return `ej: Plan de entrenamiento individualizado para ${currentMember.value.fullName} (opcional)`;
      }
      return 'ej: 3 días por semana, descanso de 60s entre series (opcional)';
    });

    // 6. Carga Asíncrona del Catálogo de Ejercicios por Zona y Texto
    const fetchCatalog = async (zone, query = '') => {
      isLoadingCatalog.value = true;
      try {
        const params = {};
        if (zone && zone !== 'all') {
          params.muscle = zone;
        }
        if (query && query.trim().length > 0) {
          params.q = query.trim();
        }

        const response = await axios.get('/api/exercises/search', { params });
        if (response.data && response.data.success) {
          catalogExercises.value = response.data.data || [];
        } else {
          catalogExercises.value = [];
        }
      } catch (err) {
        console.error('Error al cargar ejercicios del catálogo:', err);
        showNotification('No se pudo conectar con el catálogo de ejercicios.', 'danger');
        catalogExercises.value = [];
      } finally {
        isLoadingCatalog.value = false;
      }
    };

    const selectZone = (zoneId) => {
      activeZone.value = zoneId;
      fetchCatalog(zoneId, searchQuery.value);
    };

    const onSearchInput = () => {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => {
        fetchCatalog(activeZone.value, searchQuery.value);
      }, 250);
    };

    const clearSearch = () => {
      searchQuery.value = '';
      fetchCatalog(activeZone.value, '');
    };

    // 7. Acciones de Prescripción (Agregar, Quitar, Reordenar)
    const isExerciseInRoutine = (exerciseId) => {
      return selectedExercises.value.some(e => e.exerciseId === exerciseId);
    };

    const addExercise = (exercise) => {
      if (isExerciseInRoutine(exercise.id)) {
        showNotification(`'${exercise.name}' ya forma parte de esta rutina. Puedes ajustar sus series.`, 'warning');
        return;
      }

      const nextOrder = selectedExercises.value.length + 1;
      selectedExercises.value.push({
        exerciseId: exercise.id,
        exerciseName: exercise.name,
        muscleGroup: exercise.muscleGroup,
        difficulty: exercise.difficulty,
        imagePath: exercise.imagePath,
        order: nextOrder,
        sets: 3,
        repetitions: 10,
        restSeconds: 60,
        notes: ''
      });

      showNotification(`'${exercise.name}' agregado a la prescripción.`, 'success');
    };

    const removeExercise = (index) => {
      selectedExercises.value.splice(index, 1);
      selectedExercises.value.forEach((item, idx) => {
        item.order = idx + 1;
      });
      showNotification('Ejercicio quitado de la rutina.', 'info');
    };

    const moveUp = (index) => {
      if (index <= 0) return;
      const temp = selectedExercises.value[index];
      selectedExercises.value[index] = selectedExercises.value[index - 1];
      selectedExercises.value[index - 1] = temp;
      selectedExercises.value.forEach((item, idx) => {
        item.order = idx + 1;
      });
    };

    const moveDown = (index) => {
      if (index >= selectedExercises.value.length - 1) return;
      const temp = selectedExercises.value[index];
      selectedExercises.value[index] = selectedExercises.value[index + 1];
      selectedExercises.value[index + 1] = temp;
      selectedExercises.value.forEach((item, idx) => {
        item.order = idx + 1;
      });
    };

    // 8. Métricas Dinámicas de Cargas
    const totalSeries = computed(() => {
      return selectedExercises.value.reduce((acc, curr) => acc + (Number(curr.sets) || 0), 0);
    });

    // 9. Validación Defensiva antes del Envío MVC
    const validateBeforeSubmit = (event) => {
      // Si el usuario no especificó un nombre pero eligió alumno, asignamos uno limpio automáticamente
      if (!routineName.value || routineName.value.trim().length === 0) {
        if (currentMember.value) {
          routineName.value = `Rutina - ${currentMember.value.fullName}`;
        } else {
          routineName.value = `Rutina General`;
        }
      }

      if (selectedExercises.value.length === 0) {
        event.preventDefault();
        showNotification('Debes agregar al menos un ejercicio a la rutina antes de guardar.', 'danger');
        return false;
      }

      return true;
    };

    // Al montar el componente, cargar ejercicios de la zona activa
    onMounted(() => {
      fetchCatalog(activeZone.value, '');
    });

    return {
      isEditing,
      availableMembers,
      selectedMemberId,
      currentMember,
      selectedMemberName,
      selectedMemberEmail,
      selectedMemberAvatar,
      onMemberChanged,
      routineName,
      routineDescription,
      computedNamePlaceholder,
      computedDescPlaceholder,
      zones,
      activeZone,
      selectZone,
      searchQuery,
      catalogExercises,
      isLoadingCatalog,
      onSearchInput,
      clearSearch,
      selectedExercises,
      isExerciseInRoutine,
      addExercise,
      removeExercise,
      moveUp,
      moveDown,
      totalSeries,
      notification,
      validateBeforeSubmit
    };
  }
});

routineApp.mount('#routine-builder');
