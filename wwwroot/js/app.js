// jQuery-based SPA-lite: jQuery handles DOM updates/events/AJAX; Bootstrap 5's own JS
// (not a jQuery plugin) handles the modal/toast widgets. Server-side validation
// (DnsZoneService) is the source of truth — this file only gives the customer instant,
// friendly feedback and reflects whatever the server says.

var state = { zones: [], currentZoneId: null };

var $zoneList = $('#zoneList');
var $emptyState = $('#emptyState');
var $zoneDetail = $('#zoneDetail');
var $zoneName = $('#zoneName');
var $zoneMeta = $('#zoneMeta');
var $recordRows = $('#recordRows');
var $zoneModalTitle = $('#zoneModalTitle');
var $zoneSubmitBtn = $('#zoneSubmitBtn');
var editingZoneId = null;

var zoneModal = new bootstrap.Modal(document.getElementById('zoneModal'));
var $zoneForm = $('#zoneForm');
var $zoneNameInput = $('#zoneNameInput');
var $zoneFormError = $('#zoneFormError');

var recordModal = new bootstrap.Modal(document.getElementById('recordModal'));
var $recordForm = $('#recordForm');
var $recordFormError = $('#recordFormError');
var $recordIdInput = $('#recordId');
var $recordNameInput = $('#recordNameInput');
var $recordTypeInput = $('#recordTypeInput');
var $recordTtlInput = $('#recordTtlInput');
var $recordDataInput = $('#recordDataInput');
var $recordDataHint = $('#recordDataHint');
var $recordModalTitle = $('#recordModalTitle');
var $recordSubmitBtn = $('#recordSubmitBtn');

var $toast = $('#toast');
var $toastBody = $('#toastBody');
var toast = new bootstrap.Toast($toast[0], { delay: 3500 });

var DATA_HINTS = {
  A: 'IPv4 address, e.g. 192.0.2.10',
  AAAA: 'IPv6 address, e.g. 2001:db8::1',
  CNAME: 'Target hostname, e.g. target.example.com',
  NS: 'Nameserver hostname, e.g. ns1.example.com',
  TXT: 'Free-form text, up to 255 characters',
};

function showToast(message, isError) {
  $toastBody.text(message);
  $toast.removeClass('bg-success bg-danger').addClass(isError ? 'bg-danger' : 'bg-success');
  toast.show();
}

function escapeHtml(str) {
  return $('<div>').text(str).html();
}

function api(path, options) {
  options = options || {};
  return $.ajax({
    url: '/api' + path,
    method: options.method || 'GET',
    contentType: 'application/json',
    data: options.data ? JSON.stringify(options.data) : undefined,
    dataType: 'json',
  }).catch(function (xhr) {
    var message = 'Something went wrong. Please try again.';
    if (xhr.responseJSON && xhr.responseJSON.errors) {
      message = xhr.responseJSON.errors.join(' ');
    }
    return $.Deferred().reject(new Error(message)).promise();
  });
}

function loadZones() {
  return api('/zones').then(function (zones) {
    state.zones = zones;
    renderZoneList();
  });
}

function renderZoneList() {
  $zoneList.empty();
  if (state.zones.length === 0) {
    $zoneList.html('<p class="text-muted small px-1">No zones yet — create one to get started.</p>');
    return;
  }
  state.zones.forEach(function (zone) {
    var $item = $('<button type="button" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center"></button>');
    if (zone.id === state.currentZoneId) $item.addClass('active-zone');
    $item.html(
      '<span>' + escapeHtml(zone.name) + '</span>' +
      '<span class="badge bg-secondary rounded-pill">' + zone.recordCount + '</span>'
    );
    $item.on('click', function () {
      selectZone(zone.id);
    });
    $zoneList.append($item);
  });
}

function selectZone(id) {
  state.currentZoneId = id;
  renderZoneList();

  return api('/zones/' + id).then(function (zone) {
    $emptyState.addClass('d-none');
    $zoneDetail.removeClass('d-none');
    $zoneName.text(zone.name);
    $zoneMeta.text(zone.records.length + ' of 10 records used');

    $recordRows.empty();
    zone.records.forEach(function (record) {
      var $tr = $('<tr></tr>');
      $tr.html(
        '<td>' + escapeHtml(record.name) + '</td>' +
        '<td><span class="badge bg-light text-dark border">' + record.type + '</span></td>' +
        '<td>' + record.ttl + '</td>' +
        '<td><code class="record-data">' + escapeHtml(record.data) + '</code></td>' +
        '<td class="text-end">' +
        '  <button class="btn btn-sm btn-outline-secondary me-1 edit-btn">Edit</button>' +
        '  <button class="btn btn-sm btn-outline-danger delete-btn">Delete</button>' +
        '</td>'
      );
      $tr.find('.edit-btn').on('click', function () {
        openRecordModal(record);
      });
      $tr.find('.delete-btn').on('click', function () {
        deleteRecord(record.id, record.name, record.type);
      });
      $recordRows.append($tr);
    });
  });
}

// --- Zone creation / deletion ---

$('#newZoneBtn').on('click', function () {
  editingZoneId = null;
  $zoneModalTitle.text('New Zone');
  $zoneSubmitBtn.text('Create Zone');
  $zoneForm[0].reset();
  $zoneFormError.addClass('d-none');
  zoneModal.show();
});

$('#editZoneBtn').on('click', function () {
  var zone = state.zones.find(function (z) { return z.id === state.currentZoneId; });
  if (!zone) return;
  editingZoneId = zone.id;
  $zoneModalTitle.text('Edit Zone');
  $zoneSubmitBtn.text('Update Zone');
  $zoneFormError.addClass('d-none');
  $zoneNameInput.val(zone.name);
  zoneModal.show();
});

$zoneForm.on('submit', function (e) {
  e.preventDefault();
  $zoneFormError.addClass('d-none');

  var name = $zoneNameInput.val().trim();
  var request = editingZoneId
    ? api('/zones/' + editingZoneId, { method: 'PUT', data: { name: name } })
    : api('/zones', { method: 'POST', data: { name: name } });

  request
    .then(function (zone) {
      return loadZones().then(function () {
        zoneModal.hide();
        showToast(editingZoneId ? 'Zone renamed to "' + zone.name + '".' : 'Zone "' + zone.name + '" created with 4 default NS records.');
        return selectZone(zone.id);
      });
    })
    .catch(function (err) {
      $zoneFormError.text(err.message).removeClass('d-none');
    });
});

$('#deleteZoneBtn').on('click', function () {
  var zone = state.zones.find(function (z) {
    return z.id === state.currentZoneId;
  });
  if (!zone) return;
  if (!confirm('Delete zone "' + zone.name + '" and all ' + zone.recordCount + ' of its records? This cannot be undone.')) return;

  api('/zones/' + zone.id, { method: 'DELETE' })
    .then(function () {
      state.currentZoneId = null;
      $zoneDetail.addClass('d-none');
      $emptyState.removeClass('d-none');
      return loadZones();
    })
    .then(function () {
      showToast('Zone "' + zone.name + '" deleted.');
    })
    .catch(function (err) {
      showToast(err.message, true);
    });
});

// --- Record creation / edit / deletion ---

$('#newRecordBtn').on('click', function () {
  openRecordModal(null);
});

$recordTypeInput.on('change', updateDataHint);
function updateDataHint() {
  $recordDataHint.text(DATA_HINTS[$recordTypeInput.val()] || '');
}

function openRecordModal(record) {
  $recordForm[0].reset();
  $recordFormError.addClass('d-none');

  if (record) {
    $recordModalTitle.text('Edit Record');
    $recordSubmitBtn.text('Save Changes');
    $recordIdInput.val(record.id);
    $recordNameInput.val(record.name);
    $recordTypeInput.val(record.type);
    $recordTtlInput.val(record.ttl);
    $recordDataInput.val(record.data);
  } else {
    $recordModalTitle.text('New Record');
    $recordSubmitBtn.text('Add Record');
    $recordIdInput.val('');
    $recordTtlInput.val(3600);
  }
  updateDataHint();
  recordModal.show();
}

$recordForm.on('submit', function (e) {
  e.preventDefault();
  $recordFormError.addClass('d-none');

  var payload = {
    name: $recordNameInput.val().trim() || '@',
    type: $recordTypeInput.val(),
    ttl: parseInt($recordTtlInput.val(), 10),
    data: $recordDataInput.val().trim(),
  };
  var id = $recordIdInput.val();

  var request = id
    ? api('/zones/' + state.currentZoneId + '/records/' + id, { method: 'PUT', data: payload })
    : api('/zones/' + state.currentZoneId + '/records', { method: 'POST', data: payload });

  request
    .then(function () {
      showToast(id ? 'Record updated.' : 'Record added.');
      recordModal.hide();
      return loadZones().then(function () {
        return selectZone(state.currentZoneId);
      });
    })
    .catch(function (err) {
      $recordFormError.text(err.message).removeClass('d-none');
    });
});

function deleteRecord(id, name, type) {
  if (!confirm('Delete the ' + type + ' record "' + name + '"?')) return;

  api('/zones/' + state.currentZoneId + '/records/' + id, { method: 'DELETE' })
    .then(function () {
      showToast('Record deleted.');
      return loadZones().then(function () {
        return selectZone(state.currentZoneId);
      });
    })
    .catch(function (err) {
      showToast(err.message, true);
    });
}

$(function () {
  loadZones();
});
