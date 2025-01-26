$(document).ready(function () {
    // When a competition button is clicked
    $('.btn-open-modal').click(function () {
        const competitionId = $(this).data('id');
        const competitionName = $(this).data('name');

        // Set the modal title
        $('#yearsModalLabel').text(`Години`);

        // Fetch years for the competition
        $.ajax({
            url: `/api/competitions/${competitionId}/years`, // Update with your API endpoint
            type: 'GET',
            success: function (data) {
                const yearsList = $('#yearsList');
                yearsList.empty(); // Clear previous years

                // Populate the list with years
                data.forEach(year => {
                    const yearItem = `
                                    <li class="list-group-item">
                                        <a href="/Competitions/${competitionId}/${year.item1}/index" >
                                            ${year.item2}
                                        </a>
                                    </li>`;
                    yearsList.append(yearItem);


                });
                const yearItemLast = `
                                    <li class="list-group-item">
                        <a href="/YearCompetitions/Create/${competitionId}">+</a>
                    </li>`;
                yearsList.append(yearItemLast);
            },
            error: function (xhr, status, error) {
                console.error('Error fetching years:', error);
            }
        });
    });
});