/**
 * FitNet - Módulo Reactivo ABM de Ejercicios (Fase 8 - Tarea 8.4)
 * Stack: Vue 3 Composition API (Global Build) + Axios Local
 * Montaje: <div id="app-ejercicios">
 * Responsabilidades:
 * 1. Paginación y filtrado 100% reactivos conectados con ExercisesApiController.
 * 2. Modal reactivo Bootstrap para Alta y Edición sin recarga de página.
 * 3. Carga y previsualización de imágenes técnicas vía FormData multipart.
 * 4. Diálogo de confirmación para baja (lógica/física) con toasts de feedback inmediato.
 */

const { createApp, ref, reactive, computed, onMounted } = Vue;

const app = createApp({
  template: `
    <div>
      <!-- 1. Tarjeta Principal de Administración -->
      <div class="fn-card">
        <!-- Header con Acciones -->
        <div class="fn-card-header d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
          <div class="fn-card-title">
            <i class="fa-solid fa-list-check text-primary-fn me-2"></i>
            <span>Catálogo Maestro de Ejercicios</span>
            <span class="badge bg-dark border border-secondary ms-2 text-white font-monospace">
              {{ pagination.totalItems }} registrados
            </span>
          </div>

          <div class="d-flex align-items-center gap-2 w-100 w-md-auto justify-content-end">
            <button class="btn btn-outline-secondary btn-sm" @click="fetchExercises" :disabled="loading" title="Recargar listado">
              <i class="fa-solid fa-rotate" :class="{ 'fa-spin': loading }"></i>
            </button>
            <button class="btn btn-fn-primary btn-sm d-flex align-items-center gap-2" @click="openCreateModal">
              <i class="fa-solid fa-plus"></i>
              <span>Nuevo Ejercicio</span>
            </button>
          </div>
        </div>

        <!-- Barra de Filtros y Búsqueda -->
        <div class="p-3 border-bottom border-secondary border-opacity-25 bg-surface-fn">
          <div class="row g-2">
            <!-- Búsqueda textual -->
            <div class="col-12 col-md-4">
              <div class="input-group input-group-sm">
                <span class="input-group-text bg-dark border-secondary border-opacity-50 text-muted">
                  <i class="fa-solid fa-magnifying-glass"></i>
                </span>
                <input
                  type="text"
                  class="form-control bg-dark border-secondary border-opacity-50 text-white"
                  placeholder="Buscar por nombre o descripción..."
                  v-model="filters.search"
                  @input="onSearchChange"
                />
                <button
                  v-if="filters.search"
                  class="btn btn-outline-secondary border-opacity-50"
                  type="button"
                  @click="filters.search = ''; onSearchChange();"
                >
                  <i class="fa-solid fa-xmark"></i>
                </button>
              </div>
            </div>

            <!-- Grupo Muscular -->
            <div class="col-6 col-md-2">
              <select class="form-select form-select-sm bg-dark border-secondary border-opacity-50 text-white" v-model="filters.muscleGroup" @change="applyFilters">
                <option value="">Grupo: Todos</option>
                <option v-for="mg in muscleGroups" :key="mg" :value="mg">{{ mg }}</option>
              </select>
            </div>

            <!-- Dificultad -->
            <div class="col-6 col-md-2">
              <select class="form-select form-select-sm bg-dark border-secondary border-opacity-50 text-white" v-model="filters.difficulty" @change="applyFilters">
                <option value="">Nivel: Todos</option>
                <option v-for="dif in difficulties" :key="dif" :value="dif">{{ dif }}</option>
              </select>
            </div>

            <!-- Visibilidad Pública / Trial -->
            <div class="col-6 col-md-2">
              <select class="form-select form-select-sm bg-dark border-secondary border-opacity-50 text-white" v-model="filters.isPublic" @change="applyFilters">
                <option value="">Visibilidad: Todas</option>
                <option value="true">Público (Trial)</option>
                <option value="false">Exclusivo (Socios)</option>
              </select>
            </div>

            <!-- Estado Activo / Inactivo -->
            <div class="col-6 col-md-2 d-flex gap-2">
              <select class="form-select form-select-sm bg-dark border-secondary border-opacity-50 text-white" v-model="filters.isActive" @change="applyFilters">
                <option value="">Estado: Todos</option>
                <option value="true">Activos</option>
                <option value="false">Inactivos</option>
              </select>
              <button class="btn btn-outline-secondary btn-sm" @click="resetFilters" title="Limpiar filtros">
                <i class="fa-solid fa-filter-circle-xmark"></i>
              </button>
            </div>
          </div>
        </div>

        <!-- Tabla Reactiva de Ejercicios -->
        <div class="fn-card-body p-0 position-relative">
          <!-- Spinner de carga superpuesto -->
          <div v-if="loading" class="position-absolute w-100 h-100 top-0 start-0 d-flex align-items-center justify-content-center bg-dark bg-opacity-50" style="z-index: 5;">
            <div class="spinner-border text-primary-fn" role="status">
              <span class="visually-hidden">Cargando...</span>
            </div>
          </div>

          <div class="table-responsive">
            <table class="table table-dark table-hover align-middle mb-0" style="background: transparent;">
              <thead class="bg-surface-fn font-heading text-primary-fn small text-uppercase">
                <tr class="border-bottom border-secondary border-opacity-25">
                  <th class="ps-4" style="width: 70px;">Foto</th>
                  <th @click="toggleSort('name')" style="cursor: pointer;">
                    Nombre
                    <i class="fa-solid fa-xs ms-1" :class="filters.sortBy === 'name' ? (filters.sortDesc ? 'fa-sort-down' : 'fa-sort-up') : 'fa-sort text-muted'"></i>
                  </th>
                  <th @click="toggleSort('musclegroup')" style="cursor: pointer;">
                    Grupo
                    <i class="fa-solid fa-xs ms-1" :class="filters.sortBy === 'musclegroup' ? (filters.sortDesc ? 'fa-sort-down' : 'fa-sort-up') : 'fa-sort text-muted'"></i>
                  </th>
                  <th @click="toggleSort('difficulty')" style="cursor: pointer;">
                    Dificultad
                    <i class="fa-solid fa-xs ms-1" :class="filters.sortBy === 'difficulty' ? (filters.sortDesc ? 'fa-sort-down' : 'fa-sort-up') : 'fa-sort text-muted'"></i>
                  </th>
                  <th>Alcance</th>
                  <th>Estado</th>
                  <th>Rutinas</th>
                  <th class="text-end pe-4" style="min-width: 130px;">Acciones</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="ex in exercises" :key="ex.id" :class="{ 'opacity-50': !ex.isActive }">
                  <!-- Imagen Técnica -->
                  <td class="ps-4">
                    <img
                      v-if="ex.imagePath"
                      :src="ex.imagePath"
                      :alt="ex.name"
                      class="rounded border border-secondary border-opacity-50"
                      style="width: 44px; height: 44px; object-fit: cover;"
                    />
                    <div
                      v-else
                      class="rounded bg-surface-fn border border-secondary border-opacity-25 d-inline-flex align-items-center justify-content-center text-muted"
                      style="width: 44px; height: 44px; font-size: 1.1rem;"
                    >
                      <i class="fa-solid fa-dumbbell"></i>
                    </div>
                  </td>

                  <!-- Nombre y Descripción -->
                  <td>
                    <span class="d-block text-white fw-bold">{{ ex.name }}</span>
                    <span class="text-muted small text-truncate d-block" style="max-width: 280px;">
                      {{ ex.description || 'Sin descripción técnica registrada.' }}
                    </span>
                  </td>

                  <!-- Grupo Muscular -->
                  <td>
                    <span class="badge bg-surface-fn text-white border border-secondary border-opacity-50 font-heading">
                      {{ ex.muscleGroup }}
                    </span>
                  </td>

                  <!-- Dificultad -->
                  <td>
                    <span
                      class="badge"
                      :class="{
                        'bg-success bg-opacity-25 text-success border border-success border-opacity-50': ex.difficulty === 'Principiante',
                        'bg-warning bg-opacity-25 text-warning border border-warning border-opacity-50': ex.difficulty === 'Intermedio',
                        'bg-danger bg-opacity-25 text-danger border border-danger border-opacity-50': ex.difficulty === 'Avanzado'
                      }"
                    >
                      {{ ex.difficulty }}
                    </span>
                  </td>

                  <!-- Alcance / Visibilidad -->
                  <td>
                    <span v-if="ex.isPublic" class="badge bg-info bg-opacity-25 text-info border border-info border-opacity-50">
                      <i class="fa-solid fa-eye me-1"></i> Público
                    </span>
                    <span v-else class="badge bg-secondary bg-opacity-25 text-light border border-secondary border-opacity-50">
                      <i class="fa-solid fa-lock me-1"></i> Exclusivo
                    </span>
                  </td>

                  <!-- Estado -->
                  <td>
                    <span v-if="ex.isActive" class="badge bg-success bg-opacity-25 text-success border border-success border-opacity-50">
                      <i class="fa-solid fa-check me-1"></i> Activo
                    </span>
                    <span v-else class="badge bg-danger bg-opacity-25 text-danger border border-danger border-opacity-50">
                      <i class="fa-solid fa-ban me-1"></i> Inactivo
                    </span>
                  </td>

                  <!-- Conteo de Rutinas -->
                  <td>
                    <span class="badge bg-dark border border-secondary text-muted" :class="{ 'text-primary-fn border-primary-fn': ex.workoutsCount > 0 }">
                      <i class="fa-solid fa-clipboard-list me-1"></i> {{ ex.workoutsCount }}
                    </span>
                  </td>

                  <!-- Botones de Acción -->
                  <td class="text-end pe-4">
                    <div class="btn-group btn-group-sm">
                      <button class="btn btn-outline-secondary text-white" @click="openEditModal(ex)" title="Editar ejercicio">
                        <i class="fa-solid fa-pen-to-square"></i>
                      </button>
                      <button class="btn btn-outline-danger" @click="confirmDelete(ex)" title="Eliminar o dar de baja">
                        <i class="fa-solid fa-trash-can"></i>
                      </button>
                    </div>
                  </td>
                </tr>

                <!-- Estado Vacío -->
                <tr v-if="exercises.length === 0 && !loading">
                  <td colspan="8" class="text-center py-5 text-muted">
                    <i class="fa-solid fa-dumbbell fa-2x mb-2 d-block text-secondary"></i>
                    <span class="font-heading">No se encontraron ejercicios con los filtros seleccionados.</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Paginador Server-Side Reactivo -->
        <div class="fn-card-footer d-flex flex-column flex-md-row justify-content-between align-items-center gap-3 p-3 border-top border-secondary border-opacity-25 bg-surface-fn">
          <div class="text-muted small">
            Mostrando página <strong class="text-white">{{ pagination.page }}</strong> de <strong class="text-white">{{ pagination.totalPages || 1 }}</strong>
            ({{ pagination.totalItems }} resultados en total)
          </div>

          <div class="d-flex align-items-center gap-3">
            <div class="d-flex align-items-center gap-2">
              <span class="text-muted small">Por pág:</span>
              <select class="form-select form-select-sm bg-dark border-secondary border-opacity-50 text-white" style="width: 70px;" v-model.number="filters.pageSize" @change="applyFilters">
                <option :value="5">5</option>
                <option :value="10">10</option>
                <option :value="20">20</option>
                <option :value="50">50</option>
              </select>
            </div>

            <nav aria-label="Paginación de ejercicios">
              <ul class="pagination pagination-sm mb-0">
                <li class="page-item" :class="{ disabled: !pagination.hasPreviousPage }">
                  <button class="page-link bg-dark border-secondary border-opacity-50 text-white" @click="changePage(1)" title="Primera">
                    <i class="fa-solid fa-angles-left"></i>
                  </button>
                </li>
                <li class="page-item" :class="{ disabled: !pagination.hasPreviousPage }">
                  <button class="page-link bg-dark border-secondary border-opacity-50 text-white" @click="changePage(pagination.page - 1)" title="Anterior">
                    <i class="fa-solid fa-chevron-left"></i>
                  </button>
                </li>
                <li class="page-item active">
                  <span class="page-link bg-primary-fn text-dark fw-bold border-primary-fn">
                    {{ pagination.page }}
                  </span>
                </li>
                <li class="page-item" :class="{ disabled: !pagination.hasNextPage }">
                  <button class="page-link bg-dark border-secondary border-opacity-50 text-white" @click="changePage(pagination.page + 1)" title="Siguiente">
                    <i class="fa-solid fa-chevron-right"></i>
                  </button>
                </li>
                <li class="page-item" :class="{ disabled: !pagination.hasNextPage }">
                  <button class="page-link bg-dark border-secondary border-opacity-50 text-white" @click="changePage(pagination.totalPages)" title="Última">
                    <i class="fa-solid fa-angles-right"></i>
                  </button>
                </li>
              </ul>
            </nav>
          </div>
        </div>
      </div>

      <!-- ==========================================
           MODAL 1: Crear / Editar Ejercicio (Bootstrap)
           ========================================== -->
      <div class="modal fade" id="modalEjercicio" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg modal-dialog-centered">
          <div class="modal-content bg-surface-fn text-white border border-secondary border-opacity-25">
            <div class="modal-header border-bottom border-secondary border-opacity-25">
              <h5 class="modal-title font-heading text-primary-fn">
                <i class="fa-solid me-2" :class="modalMode === 'create' ? 'fa-plus' : 'fa-pen-to-square'"></i>
                {{ modalMode === 'create' ? 'Crear Nuevo Ejercicio' : 'Modificar Ejercicio' }}
              </h5>
              <button type="button" class="btn-close btn-close-white" @click="closeModal" :disabled="saving"></button>
            </div>

            <form @submit.prevent="saveExercise">
              <div class="modal-body">
                <div class="row g-3">
                  <!-- Nombre del Ejercicio -->
                  <div class="col-12 col-md-6">
                    <label class="form-label small font-heading text-uppercase text-muted">Nombre del Ejercicio <span class="text-danger">*</span></label>
                    <input
                      type="text"
                      class="form-control bg-dark border-secondary border-opacity-50 text-white"
                      :class="{ 'is-invalid': formErrors.Name }"
                      v-model="form.name"
                      placeholder="Ej. Press de Banca Plano"
                      required
                    />
                    <div class="invalid-feedback" v-if="formErrors.Name">{{ formErrors.Name[0] }}</div>
                  </div>

                  <!-- Grupo Muscular -->
                  <div class="col-12 col-md-3">
                    <label class="form-label small font-heading text-uppercase text-muted">Grupo Muscular <span class="text-danger">*</span></label>
                    <select
                      class="form-select bg-dark border-secondary border-opacity-50 text-white"
                      :class="{ 'is-invalid': formErrors.MuscleGroup }"
                      v-model="form.muscleGroup"
                      required
                    >
                      <option value="" disabled>Seleccione...</option>
                      <option v-for="mg in muscleGroups" :key="mg" :value="mg">{{ mg }}</option>
                    </select>
                    <div class="invalid-feedback" v-if="formErrors.MuscleGroup">{{ formErrors.MuscleGroup[0] }}</div>
                  </div>

                  <!-- Dificultad -->
                  <div class="col-12 col-md-3">
                    <label class="form-label small font-heading text-uppercase text-muted">Dificultad <span class="text-danger">*</span></label>
                    <select
                      class="form-select bg-dark border-secondary border-opacity-50 text-white"
                      :class="{ 'is-invalid': formErrors.Difficulty }"
                      v-model="form.difficulty"
                      required
                    >
                      <option value="" disabled>Seleccione...</option>
                      <option v-for="dif in difficulties" :key="dif" :value="dif">{{ dif }}</option>
                    </select>
                    <div class="invalid-feedback" v-if="formErrors.Difficulty">{{ formErrors.Difficulty[0] }}</div>
                  </div>

                  <!-- Descripción Técnica -->
                  <div class="col-12">
                    <label class="form-label small font-heading text-uppercase text-muted">Descripción / Indicaciones Técnicas</label>
                    <textarea
                      class="form-control bg-dark border-secondary border-opacity-50 text-white"
                      :class="{ 'is-invalid': formErrors.Description }"
                      v-model="form.description"
                      rows="2"
                      placeholder="Postura adecuada, trayectoria de la barra, respiración y precauciones anatómicas..."
                    ></textarea>
                    <div class="invalid-feedback" v-if="formErrors.Description">{{ formErrors.Description[0] }}</div>
                  </div>

                  <!-- URL de Video Tutorial (Opcional) -->
                  <div class="col-12 col-md-6">
                    <label class="form-label small font-heading text-uppercase text-muted">URL Demostrativa de Video (YouTube / MP4)</label>
                    <div class="input-group">
                      <span class="input-group-text bg-dark border-secondary border-opacity-50 text-muted">
                        <i class="fa-solid fa-video"></i>
                      </span>
                      <input
                        type="url"
                        class="form-control bg-dark border-secondary border-opacity-50 text-white"
                        :class="{ 'is-invalid': formErrors.VideoUrl }"
                        v-model="form.videoUrl"
                        placeholder="https://youtube.com/watch?v=..."
                      />
                    </div>
                    <div class="invalid-feedback" v-if="formErrors.VideoUrl">{{ formErrors.VideoUrl[0] }}</div>
                  </div>

                  <!-- Imagen Técnica con Previsualización -->
                  <div class="col-12 col-md-6">
                    <label class="form-label small font-heading text-uppercase text-muted">Foto / Demostración Técnica</label>
                    <input
                      type="file"
                      ref="fileInputRef"
                      class="form-control bg-dark border-secondary border-opacity-50 text-white"
                      accept=".jpg,.jpeg,.png,.webp"
                      @change="onFileSelected"
                    />
                    <small class="text-muted d-block mt-1">Formatos: JPG, PNG, WEBP (máximo 2 MB).</small>

                    <!-- Preview reactivo -->
                    <div v-if="imagePreview || currentImagePath" class="mt-2 d-flex align-items-center gap-2 p-2 rounded bg-dark border border-secondary border-opacity-25">
                      <img :src="imagePreview || currentImagePath" alt="Vista previa" class="rounded" style="width: 50px; height: 50px; object-fit: cover;" />
                      <div class="small flex-grow-1">
                        <span class="d-block text-white fw-bold">{{ imagePreview ? 'Nueva imagen seleccionada' : 'Imagen actual en servidor' }}</span>
                        <span class="text-muted" style="font-size: 0.75rem;">Lista para guardar</span>
                      </div>
                      <button type="button" class="btn btn-outline-danger btn-sm" @click="removeSelectedImage" title="Quitar imagen">
                        <i class="fa-solid fa-xmark"></i>
                      </button>
                    </div>
                  </div>

                  <!-- Switches de Visibilidad y Estado -->
                  <div class="col-12 col-md-6">
                    <div class="form-check form-switch mt-2">
                      <input class="form-check-input" type="checkbox" id="switchIsPublic" v-model="form.isPublic" />
                      <label class="form-check-label text-white" for="switchIsPublic">
                        <strong>Visible en período de prueba gratuito (Trial 24h)</strong>
                      </label>
                      <small class="d-block text-muted">Si está desmarcado, solo socios con planes Gold o Premium activos podrán ver este ejercicio.</small>
                    </div>
                  </div>

                  <div class="col-12 col-md-6">
                    <div class="form-check form-switch mt-2">
                      <input class="form-check-input" type="checkbox" id="switchIsActive" v-model="form.isActive" />
                      <label class="form-check-label text-white" for="switchIsActive">
                        <strong>Ejercicio Habilitado / Activo</strong>
                      </label>
                      <small class="d-block text-muted">Los ejercicios inactivos quedan ocultos para socios y para nuevas rutinas.</small>
                    </div>
                  </div>
                </div>
              </div>

              <div class="modal-footer border-top border-secondary border-opacity-25">
                <button type="button" class="btn btn-outline-secondary" @click="closeModal" :disabled="saving">Cancelar</button>
                <button type="submit" class="btn btn-fn-primary d-flex align-items-center gap-2" :disabled="saving">
                  <span v-if="saving" class="spinner-border spinner-border-sm" role="status"></span>
                  <i v-else class="fa-solid fa-floppy-disk"></i>
                  <span>{{ saving ? 'Guardando...' : (modalMode === 'create' ? 'Crear Ejercicio' : 'Guardar Cambios') }}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <!-- ==========================================
           MODAL 2: Confirmación de Baja (Bootstrap)
           ========================================== -->
      <div class="modal fade" id="modalEliminar" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content bg-surface-fn text-white border border-danger border-opacity-50">
            <div class="modal-header border-bottom border-secondary border-opacity-25">
              <h5 class="modal-title font-heading text-danger">
                <i class="fa-solid fa-triangle-exclamation me-2"></i>
                Confirmar Baja de Ejercicio
              </h5>
              <button type="button" class="btn-close btn-close-white" @click="closeDeleteModal" :disabled="deleting"></button>
            </div>

            <div class="modal-body" v-if="exerciseToDelete">
              <p class="text-white mb-3">
                ¿Estás seguro de que deseas eliminar el ejercicio <strong class="text-primary-fn">{{ exerciseToDelete.name }}</strong>?
              </p>

              <!-- Alerta inteligente según dependencias (Regla de negocio) -->
              <div v-if="exerciseToDelete.workoutsCount > 0" class="alert alert-warning border border-warning border-opacity-50 bg-dark text-warning small mb-0">
                <i class="fa-solid fa-shield-halved me-1"></i>
                <strong>Atención:</strong> Este ejercicio está vinculado a <strong>{{ exerciseToDelete.workoutsCount }} rutina(s) activa(s)</strong>.
                Para no romper el plan de los socios, se aplicará una <strong>baja lógica</strong> (desactivación). Sus registros históricos no se perderán.
              </div>
              <div v-else class="alert alert-danger border border-danger border-opacity-50 bg-dark text-danger small mb-0">
                <i class="fa-solid fa-trash-can me-1"></i>
                Este ejercicio no tiene rutinas asociadas. Se realizará una <strong>baja física completa</strong>, borrando también sus fotos del disco del servidor.
              </div>
            </div>

            <div class="modal-footer border-top border-secondary border-opacity-25">
              <button type="button" class="btn btn-outline-secondary" @click="closeDeleteModal" :disabled="deleting">Cancelar</button>
              <button type="button" class="btn btn-danger d-flex align-items-center gap-2" @click="deleteExercise" :disabled="deleting">
                <span v-if="deleting" class="spinner-border spinner-border-sm" role="status"></span>
                <i v-else class="fa-solid fa-trash-can"></i>
                <span>{{ deleting ? 'Eliminando...' : 'Confirmar Baja' }}</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- ==========================================
           3. TOAST FLOTANTE REACTIVO (Notificaciones)
           ========================================== -->
      <div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index: 1090;">
        <div
          class="toast align-items-center text-white border-0 shadow-lg"
          :class="{
            'show': toast.show,
            'bg-success': toast.type === 'success',
            'bg-danger': toast.type === 'danger',
            'bg-warning text-dark': toast.type === 'warning',
            'bg-info': toast.type === 'info'
          }"
          role="alert"
          aria-live="assertive"
          aria-atomic="true"
        >
          <div class="d-flex">
            <div class="toast-body">
              <strong>{{ toast.title }}:</strong> {{ toast.message }}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" @click="closeToast"></button>
          </div>
        </div>
      </div>
    </div>
  `,
  setup() {
    // ==========================================
    // 1. Constantes del Dominio
    // ==========================================
    const muscleGroups = ['Pecho', 'Espalda', 'Piernas', 'Hombros', 'Brazos', 'Core'];
    const difficulties = ['Principiante', 'Intermedio', 'Avanzado'];

    // ==========================================
    // 2. Estado Reactivo de Listado y Paginación
    // ==========================================
    const exercises = ref([]);
    const loading = ref(false);
    const saving = ref(false);
    const deleting = ref(false);
    const fileInputRef = ref(null);

    const filters = reactive({
      search: '',
      muscleGroup: '',
      difficulty: '',
      isPublic: '',
      isActive: '',
      sortBy: 'name',
      sortDesc: false,
      page: 1,
      pageSize: 10
    });

    const pagination = reactive({
      page: 1,
      pageSize: 10,
      totalItems: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false
    });

    // ==========================================
    // 3. Estado Reactivo del Modal (Crear / Editar)
    // ==========================================
    const modalMode = ref('create'); // 'create' | 'edit'

    const initialFormState = () => ({
      id: 0,
      name: '',
      description: '',
      muscleGroup: '',
      difficulty: '',
      videoUrl: '',
      isPublic: true,
      isActive: true,
      imageFile: null,
      removeCurrentImage: false
    });

    const form = reactive(initialFormState());
    const formErrors = reactive({});
    const imagePreview = ref(null);
    const currentImagePath = ref(null);

    // ==========================================
    // 4. Estado de Eliminación y Toasts
    // ==========================================
    const exerciseToDelete = ref(null);

    const toast = reactive({
      show: false,
      title: '',
      message: '',
      type: 'success' // 'success' | 'danger' | 'warning' | 'info'
    });

    let toastTimer = null;
    const showToast = (message, type = 'success', title = '') => {
      if (toastTimer) clearTimeout(toastTimer);
      toast.show = true;
      toast.message = message;
      toast.type = type;
      toast.title = title || (type === 'success' ? 'Éxito' : type === 'danger' ? 'Error' : 'Notificación');
      toastTimer = setTimeout(() => {
        toast.show = false;
      }, 4500);
    };

    const closeToast = () => {
      toast.show = false;
      if (toastTimer) clearTimeout(toastTimer);
    };

    // ==========================================
    // 5. Instancias de Modales Bootstrap
    // ==========================================
    const getModalInstance = (id) => {
      const el = document.getElementById(id);
      if (!el || typeof bootstrap === 'undefined') return null;
      return bootstrap.Modal.getOrCreateInstance(el);
    };

    // ==========================================
    // 6. Consultas API: Obtención de Ejercicios
    // ==========================================
    const fetchExercises = async () => {
      loading.value = true;
      try {
        const params = {
          page: filters.page,
          pageSize: filters.pageSize,
          search: filters.search ? filters.search.trim() : undefined,
          muscleGroup: filters.muscleGroup || undefined,
          difficulty: filters.difficulty || undefined,
          isPublic: filters.isPublic !== '' ? filters.isPublic === 'true' : undefined,
          isActive: filters.isActive !== '' ? filters.isActive === 'true' : undefined,
          sortBy: filters.sortBy,
          sortDesc: filters.sortDesc
        };

        const response = await axios.get('/api/exercises', { params });
        const pagedData = response.data?.data || response.data;

        exercises.value = pagedData.items || [];
        pagination.page = pagedData.page || 1;
        pagination.pageSize = pagedData.pageSize || 10;
        pagination.totalItems = pagedData.totalItems || 0;
        pagination.totalPages = pagedData.totalPages || 0;
        pagination.hasPreviousPage = pagedData.hasPreviousPage || false;
        pagination.hasNextPage = pagedData.hasNextPage || false;
      } catch (error) {
        console.error('Error al cargar ejercicios:', error);
        const errMsg = error.response?.data?.message || 'No se pudieron recuperar los ejercicios.';
        showToast(errMsg, 'danger', 'Error de Conexión');
      } finally {
        loading.value = false;
      }
    };

    // Búsqueda en tiempo real con debounce (350 ms)
    let searchDebounceTimer = null;
    const onSearchChange = () => {
      if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
      searchDebounceTimer = setTimeout(() => {
        filters.page = 1;
        fetchExercises();
      }, 350);
    };

    const applyFilters = () => {
      filters.page = 1;
      fetchExercises();
    };

    const resetFilters = () => {
      filters.search = '';
      filters.muscleGroup = '';
      filters.difficulty = '';
      filters.isPublic = '';
      filters.isActive = '';
      filters.sortBy = 'name';
      filters.sortDesc = false;
      filters.page = 1;
      fetchExercises();
    };

    const toggleSort = (column) => {
      if (filters.sortBy === column) {
        filters.sortDesc = !filters.sortDesc;
      } else {
        filters.sortBy = column;
        filters.sortDesc = false;
      }
      filters.page = 1;
      fetchExercises();
    };

    const changePage = (newPage) => {
      if (newPage < 1 || newPage > pagination.totalPages || newPage === pagination.page) return;
      filters.page = newPage;
      fetchExercises();
    };

    // ==========================================
    // 7. Acciones del Modal: Alta y Modificación
    // ==========================================
    const resetFormErrors = () => {
      Object.keys(formErrors).forEach(key => delete formErrors[key]);
    };

    const openCreateModal = () => {
      modalMode.value = 'create';
      resetFormErrors();
      Object.assign(form, initialFormState());
      imagePreview.value = null;
      currentImagePath.value = null;
      if (fileInputRef.value) fileInputRef.value.value = '';

      const modal = getModalInstance('modalEjercicio');
      if (modal) modal.show();
    };

    const openEditModal = (exercise) => {
      modalMode.value = 'edit';
      resetFormErrors();
      form.id = exercise.id;
      form.name = exercise.name;
      form.description = exercise.description || '';
      form.muscleGroup = exercise.muscleGroup;
      form.difficulty = exercise.difficulty;
      form.videoUrl = exercise.videoUrl || '';
      form.isPublic = exercise.isPublic;
      form.isActive = exercise.isActive;
      form.imageFile = null;
      form.removeCurrentImage = false;

      currentImagePath.value = exercise.imagePath || null;
      imagePreview.value = null;
      if (fileInputRef.value) fileInputRef.value.value = '';

      const modal = getModalInstance('modalEjercicio');
      if (modal) modal.show();
    };

    const closeModal = () => {
      const modal = getModalInstance('modalEjercicio');
      if (modal) modal.hide();
      if (imagePreview.value && imagePreview.value.startsWith('blob:')) {
        URL.revokeObjectURL(imagePreview.value);
      }
    };

    // ==========================================
    // 8. Manejo Reactivo de Imagen Técnica
    // ==========================================
    const onFileSelected = (event) => {
      const file = event.target.files?.[0];
      if (!file) return;

      // Validación client-side previa de tamaño (máx 2MB)
      const maxBytes = 2 * 1024 * 1024;
      if (file.size > maxBytes) {
        showToast('El archivo no debe superar los 2 MB de tamaño.', 'warning', 'Archivo muy pesado');
        event.target.value = '';
        return;
      }

      // Validación de extensiones permitidas
      const allowedExt = ['.jpg', '.jpeg', '.png', '.webp'];
      const fileExt = '.' + file.name.split('.').pop().toLowerCase();
      if (!allowedExt.includes(fileExt)) {
        showToast('Formato no permitido. Utilice JPG, PNG o WEBP.', 'warning', 'Formato inválido');
        event.target.value = '';
        return;
      }

      form.imageFile = file;
      form.removeCurrentImage = false;

      // Preview local instantáneo en navegador
      if (imagePreview.value && imagePreview.value.startsWith('blob:')) {
        URL.revokeObjectURL(imagePreview.value);
      }
      imagePreview.value = URL.createObjectURL(file);
    };

    const removeSelectedImage = () => {
      form.imageFile = null;
      if (imagePreview.value && imagePreview.value.startsWith('blob:')) {
        URL.revokeObjectURL(imagePreview.value);
      }
      imagePreview.value = null;
      if (fileInputRef.value) fileInputRef.value.value = '';

      if (modalMode.value === 'edit' && currentImagePath.value) {
        form.removeCurrentImage = true;
        currentImagePath.value = null;
      }
    };

    // ==========================================
    // 9. Guardar Ejercicio (POST / PUT multipart)
    // ==========================================
    const saveExercise = async () => {
      resetFormErrors();
      saving.value = true;

      try {
        const formData = new FormData();
        formData.append('name', form.name ? form.name.trim() : '');
        formData.append('description', form.description ? form.description.trim() : '');
        formData.append('muscleGroup', form.muscleGroup);
        formData.append('difficulty', form.difficulty);
        formData.append('videoUrl', form.videoUrl ? form.videoUrl.trim() : '');
        formData.append('isPublic', form.isPublic);
        formData.append('isActive', form.isActive);

        if (form.imageFile) {
          formData.append('imageFile', form.imageFile);
        }

        let response;
        if (modalMode.value === 'create') {
          response = await axios.post('/api/exercises', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
          });
          showToast(response.data?.message || 'Ejercicio creado con éxito.', 'success');
        } else {
          formData.append('id', form.id);
          formData.append('removeCurrentImage', form.removeCurrentImage);
          response = await axios.put(`/api/exercises/${form.id}`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
          });
          showToast(response.data?.message || 'Ejercicio actualizado con éxito.', 'success');
        }

        closeModal();
        await fetchExercises();
      } catch (error) {
        console.error('Error al guardar ejercicio:', error);
        if (error.response?.status === 400 && error.response.data?.errors) {
          Object.assign(formErrors, error.response.data.errors);
          showToast('Corrija los campos marcados en el formulario.', 'danger', 'Validación');
        } else if (error.response?.status === 409) {
          const conflictMsg = error.response.data?.message || 'Ya existe un ejercicio con ese nombre.';
          formErrors['Name'] = [conflictMsg];
          showToast(conflictMsg, 'warning', 'Nombre duplicado');
        } else {
          const errMsg = error.response?.data?.message || 'Ocurrió un error inesperado al procesar la solicitud.';
          showToast(errMsg, 'danger');
        }
      } finally {
        saving.value = false;
      }
    };

    // ==========================================
    // 10. Eliminación Reactiva (Baja Controlada)
    // ==========================================
    const confirmDelete = (exercise) => {
      exerciseToDelete.value = exercise;
      const modal = getModalInstance('modalEliminar');
      if (modal) modal.show();
    };

    const closeDeleteModal = () => {
      const modal = getModalInstance('modalEliminar');
      if (modal) modal.hide();
      exerciseToDelete.value = null;
    };

    const deleteExercise = async () => {
      if (!exerciseToDelete.value) return;
      deleting.value = true;

      try {
        const id = exerciseToDelete.value.id;
        const response = await axios.delete(`/api/exercises/${id}`);
        closeDeleteModal();
        showToast(response.data?.message || 'Operación completada con éxito.', 'success');
        await fetchExercises();
      } catch (error) {
        console.error('Error al eliminar ejercicio:', error);
        const errMsg = error.response?.data?.message || 'No se pudo procesar la baja del ejercicio.';
        showToast(errMsg, 'danger', 'Error de Eliminación');
      } finally {
        deleting.value = false;
      }
    };

    // ==========================================
    // 11. Ciclo de Vida: Inicialización
    // ==========================================
    onMounted(() => {
      fetchExercises();
    });

    return {
      // Colecciones y constantes
      muscleGroups,
      difficulties,
      exercises,
      loading,
      saving,
      deleting,
      fileInputRef,

      // Paginación y filtros
      filters,
      pagination,
      onSearchChange,
      applyFilters,
      resetFilters,
      toggleSort,
      changePage,

      // Modal Create/Edit
      modalMode,
      form,
      formErrors,
      imagePreview,
      currentImagePath,
      openCreateModal,
      openEditModal,
      closeModal,
      onFileSelected,
      removeSelectedImage,
      saveExercise,

      // Modal Eliminar
      exerciseToDelete,
      confirmDelete,
      closeDeleteModal,
      deleteExercise,

      // Feedback Toasts
      toast,
      closeToast,
      fetchExercises
    };
  }
});

// Montar en el contenedor oficial de la vista Razor
document.addEventListener('DOMContentLoaded', () => {
  const mountEl = document.getElementById('app-ejercicios');
  if (mountEl) {
    app.mount('#app-ejercicios');
  }
});
