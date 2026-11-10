// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

-- Failed single imports (LTS pushes), reprocessed by the importer ImportFailureRetryService
-- One row per source/type/id (id = "<source>_<type>_<itemid>" lowercase), removed on successful import

CREATE TABLE IF NOT EXISTS public.importfailures (
	id varchar(200) NOT NULL,
	"data" jsonb NULL,
	CONSTRAINT importfailures_pkey PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS importfailures_status_idx ON public.importfailures ((data->>'Status'));
