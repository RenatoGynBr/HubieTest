/* =====================================================================
   handleController (AGENT) — TODO (candidate area)
   Open the ticket, allow assigning, replying (interact), attaching and
   changing status.
   ===================================================================== */
angular.module('hubieTest').controller('handleController',
    ['$scope', '$stateParams', 'apiService',
    function ($scope, $stateParams, apiService) {

        $scope.ticketId = parseInt($stateParams.id, 10);
        $scope.ticket = null;
        $scope.interactions = [];
        $scope.attachments = [];
        $scope.newMessage = '';

        $scope.load = function () {
            $scope.loading = true;
            $scope.error = null;

            // load ticket -> interactions -> attachments
            apiService.request('ashx/process/ticket.ashx', 'get', JSON.stringify({ ticketId: $scope.ticketId }))
                .then(function (r) {
                    $scope.ticket = r.data;
                    return apiService.request('ashx/process/ticket.ashx', 'listInteractions', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.interactions = r.data || [];
                    return apiService.request('ashx/process/ticket.ashx', 'listAttachments', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.attachments = r.data || [];
                })
                .catch(function (err) {
                    console.error('Load error', err);
                    $scope.error = (err.data && err.data.error) ? err.data.error : 'Unable to load ticket data.';
                })
                .finally(function () {
                    $scope.loading = false;
                });
        };

        $scope.assign = function () {
            $scope.actionLoading = true;
            $scope.actionError = null;

            // assign ticket to logged-in agent and reload ticket
            apiService.request('ashx/process/ticket.ashx', 'assign', JSON.stringify({ ticketId: $scope.ticketId }))
                .then(function () {
                    // reload everything to reflect new agent/status
                    $scope.load();
                })
                .catch(function (err) {
                    console.error('Assign error', err);
                    $scope.actionError = (err.data && err.data.error) ? err.data.error : 'Unable to assign ticket.';
                })
                .finally(function () {
                    $scope.actionLoading = false;
                });
        };

        $scope.reply = function () {
            var msg = ($scope.newMessage || '').trim();
            if (!msg) return;

            $scope.replyLoading = true;
            $scope.replyError = null;

            apiService.request('ashx/process/ticket.ashx', 'addInteraction', JSON.stringify({ ticketId: $scope.ticketId, message: msg }))
                .then(function () {
                    $scope.newMessage = '';
                    // reload interactions and ticket status
                    return apiService.request('ashx/process/ticket.ashx', 'listInteractions', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.interactions = r.data || [];
                    // refresh ticket header (status/agent)
                    return apiService.request('ashx/process/ticket.ashx', 'get', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.ticket = r.data;
                })
                .catch(function (err) {
                    console.error('Reply error', err);
                    $scope.replyError = (err.data && err.data.error) ? err.data.error : 'Unable to send reply.';
                })
                .finally(function () {
                    $scope.replyLoading = false;
                });
        };

        $scope.changeStatus = function (newStatus) {
            if (!newStatus) return;

            $scope.statusLoading = true;
            $scope.statusError = null;

            // change status and reload ticket
            apiService.request('ashx/process/ticket.ashx', 'changeStatus', JSON.stringify({ ticketId: $scope.ticketId, status: newStatus }))
                .then(function () {
                    return apiService.request('ashx/process/ticket.ashx', 'get', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.ticket = r.data;
                })
                .catch(function (err) {
                    console.error('ChangeStatus error', err);
                    $scope.statusError = (err.data && err.data.error) ? err.data.error : 'Unable to change status.';
                })
                .finally(function () {
                    $scope.statusLoading = false;
                });
        };

        $scope.load();
    }
]);
