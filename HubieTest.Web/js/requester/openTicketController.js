/* =====================================================================
   openTicketController (REQUESTER)
   - Load categories: IMPLEMENTED as a reference (consumes categories.ashx).
   - Save the ticket + attach a file: TODO (candidate area).
   ===================================================================== */
angular.module('hubieTest').controller('openTicketController',
    ['$scope', '$state', 'apiService',
    function ($scope, $state, apiService) {

        $scope.ticket = { TICKET_TITLE: '', TICKET_DESCRIPTION: '', CATEGORY_ID: null };
        $scope.categories = [];
        $scope.file = null;     // set by the file-input (see open-ticket.html)
        $scope.saving = false;
        $scope.error = null;

        // ---- REFERENCE: populate the category dropdown ----
        apiService.listCategories().then(function (response) {
            $scope.categories = response.data;
        });

        // ---- TODO (candidate): submit the ticket ----
        // $scope.save = function () {
        //     $scope.error = null;

        //     // Expected flow:
        //     //  1. validate title/description/category
        //     //  2. POST 'open' to ticket.ashx via apiService (add apiService.openTicket)
        //     //  3. if $scope.file is set, upload it (multipart) to attachment.ashx
        //     //  4. redirect to 'app.ticketDetail' with the returned id
        //     //
        //     // Example JSON submit (without attachment):
        //     //   apiService.request('ashx/process/ticket.ashx', 'open',
        //     //       JSON.stringify($scope.ticket)).then(...);

        //     alert('TODO: implement the ticket submission (openTicketController.save).');
        // };

        $scope.save = function () {
            $scope.error = null;

            // simple validation
            if (!$scope.ticket.TICKET_TITLE || !$scope.ticket.TICKET_DESCRIPTION || !$scope.ticket.CATEGORY_ID) {
                $scope.error = 'Title, description and category are required.';
                return;
            }

            $scope.saving = true;

            // 1) create the ticket
            apiService.request('ashx/process/ticket.ashx', 'open', JSON.stringify($scope.ticket))
                .then(function (r) {
                    var created = r.data;
                    if (!created || !created.TICKET_ID) {
                        throw new Error('Invalid response creating ticket.');
                    }
                    var ticketId = created.TICKET_ID;

                    // 2) if there is a file, upload it as multipart/form-data
                    if ($scope.file) {
                        var fd = new FormData();
                        fd.append('method', 'upload');                 // AshxBase reads "method" from form
                        fd.append('ticketId', ticketId);               // attachment handler reads ticketId from form
                        fd.append('file', $scope.file);                // request.Files[0]

                        // read token from sessionStorage to send Authorization header (same key apiService uses)
                        var token = sessionStorage.X_User_Token || sessionStorage.x_user_token || null;

                        return fetch(apiService.webServer + 'ashx/process/attachment.ashx', {
                            method: 'POST',
                            headers: token ? { 'Authorization': 'Bearer ' + token } : {},
                            body: fd
                        })
                            .then(function (resp) {
                                if (!resp.ok) throw resp;
                                return resp.json();
                            })
                            .then(function () {
                                // upload succeeded, navigate to ticket detail
                                $state.go('app.ticketDetail', { id: ticketId });
                            });
                    } else {
                        // no file, go directly to ticket detail
                        $state.go('app.ticketDetail', { id: ticketId });
                        return Promise.resolve();
                    }
                })
                .catch(function (err) {
                    console.error('save error', err);
                    // if err is a Response from fetch, try to extract JSON message
                    if (err && typeof err.json === 'function') {
                        err.json().then(function (j) { $scope.error = j.error || 'Upload failed.'; }).catch(function () { $scope.error = 'An error occurred.'; });
                    } else {
                        $scope.error = err.message || 'An error occurred while saving the ticket.';
                    }
                })
                .finally(function () {
                    $scope.saving = false;
                    // ensure Angular digest if fetch resolved outside $http (use $scope.$apply if needed)
                });
        };
    }
]);
